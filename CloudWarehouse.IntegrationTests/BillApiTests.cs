using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloudWarehouse.Backend.Models;
using CloudWarehouse.TestCommon;

namespace CloudWarehouse.IntegrationTests;

public class BillApiTests : IClassFixture<CloudWarehouseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BillApiTests(CloudWarehouseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DownloadWaybillTemplate_ReturnsXlsx()
    {
        var response = await _client.GetAsync("/api/Bill/waybill/template");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal(0x50, bytes[0]);
    }

    [Fact]
    public async Task PreviewWaybill_StandardFormat_ParsesAndRoundsWeight()
    {
        using var stream = WaybillExcelFactory.CreateStandardWorkbook();
        using var content = BuildFileContent(stream, "运单.xlsx");

        var response = await _client.PostAsync("/api/Bill/waybill/preview", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<WaybillImportResult>>(response);

        Assert.NotNull(body);
        Assert.True(body.Success, body.Message);
        Assert.NotNull(body.Data);
        Assert.Equal("标准格式", body.Data.Format);
        Assert.Single(body.Data.Rows);

        var row = body.Data.Rows[0];
        Assert.Equal("YT001", row.WaybillNo);
        Assert.Equal("安徽省", row.Province);
        Assert.Equal(2.19m, row.ActualWeight);
        Assert.Equal(3m, row.RoundedWeight);
        Assert.Equal("3", row.WeightBracketCalc);
    }

    [Fact]
    public async Task PreviewWaybill_SupplierDetailFormat_ParsesExtendedColumns()
    {
        using var stream = WaybillExcelFactory.CreateSupplierDetailWorkbook();
        using var content = BuildFileContent(stream, "账单明细.xlsx");

        var response = await _client.PostAsync("/api/Bill/waybill/preview", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<WaybillImportResult>>(response);

        Assert.True(body!.Success, body.Message);
        var row = body.Data!.Rows[0];
        Assert.Equal("SF20260120001", row.WaybillNo);
        Assert.Equal("小二小店", row.AccountName);
        Assert.Equal(5.5m, row.SourceTransitFee);
        Assert.Equal(3.5m, row.SourceLabelFee);
    }

    [Fact]
    public async Task PreviewWaybill_WhenDatabaseAvailable_CalculatesDualTrackFees()
    {
        var waybillNo = "YT-DB-" + Guid.NewGuid().ToString("N")[..8];
        var offline = await TryImportCustomerQuoteForIntegrationAsync();
        if (offline)
            return;

        // Ensure payable (cost) rules exist for the site matched by ExpressType「圆通」
        offline = await TryImportCostForIntegrationAsync();
        if (offline)
            return;

        using var stream = WaybillExcelFactory.CreateStandardWorkbook(ws =>
        {
            WaybillExcelFactory.FillStandardRow(ws, 2, waybillNo, "jiangxi", 2.19m, account: "IntegrationTestAccount");
            ws.Cell(2, 1).Value = new DateTime(2026, 6, 1);
        });
        using var content = BuildFileContent(stream, "运单.xlsx");

        var response = await _client.PostAsync("/api/Bill/waybill/preview", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<WaybillImportResult>>(response);

        Assert.True(body!.Success, body.Message);
        var row = body.Data!.Rows[0];

        Assert.Null(row.ErrorMessage);
        Assert.NotNull(row.ReceivableTotal);
        Assert.NotNull(row.PayableTotal);
        Assert.NotNull(row.Profit);
        Assert.True(row.ReceivableTotal > row.PayableTotal);
        Assert.True(row.Profit > 0);
    }

    private async Task<bool> TryImportCustomerQuoteForIntegrationAsync()
    {
        using var quoteStream = CustomerQuoteExcelFactory.CreateStandardWideWorkbook(
            province: "jiangxi", tier2to3: 12m, baseFee: 4m);
        using var quoteContent = BuildFileContent(quoteStream, "客户报价.xlsx");
        var quoteResponse = await _client.PostAsync("/api/CustomerQuote", quoteContent);
        var quoteBody = await JsonTestHelper.ReadApiAsync<ApiResponse<CustomerQuoteImportResult>>(quoteResponse);

        if (DatabaseAvailability.IsUnavailable(quoteBody))
            return true;

        Assert.True(quoteBody!.Success, quoteBody.Message);
        return false;
    }

    private async Task<bool> TryImportCostForIntegrationAsync()
    {
        // Cover both possible site codes if local DB already has 圆通-深泽
        foreach (var siteCode in new[] { "圆通-深泽", "C001", "圆通" })
        {
            using var costStream = PriceTableExcelFactory.CreateStandardFormatWorkbook(ws =>
            {
                PriceTableExcelFactory.FillSampleRow(ws, 2, "jiangxi", "jiangxi", siteCode);
                ws.Cell(2, 1).Value = new DateTime(2026, 1, 1);
                // Keep cost clearly below receivable (quote tier 2-3kg = 12)
                ws.Cell(2, 9).Value = 3.9;
                ws.Cell(2, 12).Value = 1.0;
            });
            using var costContent = BuildFileContent(costStream, "成本表.xlsx");
            var costResponse = await _client.PostAsync("/api/Import/price-table", costContent);
            var costBody = await JsonTestHelper.ReadApiAsync<ApiResponse<PriceTableImportResult>>(costResponse);

            if (DatabaseAvailability.IsUnavailable(costBody))
                return true;

            Assert.True(costBody!.Success, costBody.Message);
        }

        return false;
    }

    [Fact]
    public async Task PreviewWaybill_EmptyFile_ReturnsFailureMessage()
    {
        using var stream = new MemoryStream();
        using var content = BuildFileContent(stream, "empty.xlsx");

        var response = await _client.PostAsync("/api/Bill/waybill/preview", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<WaybillImportResult>>(response);

        Assert.False(body!.Success);
        Assert.Contains("请选择", body.Message);
    }

    [Fact]
    public async Task PreviewWaybill_InvalidExtension_ReturnsFailureMessage()
    {
        using var stream = new MemoryStream("not excel"u8.ToArray());
        using var content = BuildFileContent(stream, "bad.txt");

        var response = await _client.PostAsync("/api/Bill/waybill/preview", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<WaybillImportResult>>(response);

        Assert.False(body!.Success);
        Assert.Contains(".xlsx", body.Message);
    }

    [Fact]
    public async Task ExportWaybillResult_WithRows_ReturnsXlsxFile()
    {
        using var stream = WaybillExcelFactory.CreateStandardWorkbook();
        using var importContent = BuildFileContent(stream, "运单.xlsx");
        var importResponse = await _client.PostAsync("/api/Bill/waybill/preview", importContent);
        var importBody = await JsonTestHelper.ReadApiAsync<ApiResponse<WaybillImportResult>>(importResponse);

        var exportResponse = await _client.PostAsJsonAsync(
            "/api/Bill/waybill/export",
            importBody!.Data!.Rows,
            JsonTestHelper.Options);

        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            exportResponse.Content.Headers.ContentType?.MediaType);

        var bytes = await exportResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
    }

    [Fact]
    public async Task ExportWaybillResult_EmptyBody_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Bill/waybill/export",
            new List<WaybillImportRow>());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static MultipartFormDataContent BuildFileContent(Stream stream, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", fileName);
        return content;
    }
}
