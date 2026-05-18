using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloudWarehouse.Backend.Models;
using CloudWarehouse.TestCommon;

namespace CloudWarehouse.IntegrationTests;

public class ImportApiTests : IClassFixture<CloudWarehouseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ImportApiTests(CloudWarehouseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ImportPriceTable_ValidXlsx_ReturnsSuccessWithExpectedPrices()
    {
        using var stream = PriceTableExcelFactory.CreateValidWorkbook();
        using var content = BuildFileContent(stream, "价格表.xlsx");

        var response = await _client.PostAsync("/api/Import/price-table/preview", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<PriceTableImportResult>>(response);

        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(1, body.Data.TotalRows);
        Assert.Equal(5.6m, body.Data.Rows[0].ExpectedPrice1Kg);
    }

    [Fact]
    public async Task DownloadStandardTemplate_ReturnsXlsx()
    {
        var response = await _client.GetAsync("/api/Import/price-table/template");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
    }

    [Fact]
    public async Task ImportPriceTable_EmptyFile_ReturnsFailureMessage()
    {
        using var stream = new MemoryStream();
        using var content = BuildFileContent(stream, "empty.xlsx");

        var response = await _client.PostAsync("/api/Import/price-table", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<PriceTableImportResult>>(response);

        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Contains("请选择", body.Message);
    }

    [Fact]
    public async Task ImportPriceTable_InvalidExtension_ReturnsFailureMessage()
    {
        using var stream = new MemoryStream("not excel"u8.ToArray());
        using var content = BuildFileContent(stream, "bad.txt");

        var response = await _client.PostAsync("/api/Import/price-table", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<PriceTableImportResult>>(response);

        Assert.False(body!.Success);
        Assert.Contains(".xlsx", body.Message);
    }

    [Fact]
    public async Task ImportPriceTable_InvalidHeader_ReturnsFailureMessage()
    {
        using var stream = PriceTableExcelFactory.CreateInvalidHeaderWorkbook();
        using var content = BuildFileContent(stream, "价格表.xlsx");

        var response = await _client.PostAsync("/api/Import/price-table", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<PriceTableImportResult>>(response);

        Assert.False(body!.Success);
        Assert.Contains("无法识别", body!.Message);
    }

    [Fact]
    public async Task ExportPriceTableResult_WithRows_ReturnsXlsxFile()
    {
        using var stream = PriceTableExcelFactory.CreateValidWorkbook();
        using var importContent = BuildFileContent(stream, "价格表.xlsx");
        var importResponse = await _client.PostAsync("/api/Import/price-table/preview", importContent);
        var importBody = await JsonTestHelper.ReadApiAsync<ApiResponse<PriceTableImportResult>>(importResponse);

        var exportResponse = await _client.PostAsJsonAsync(
            "/api/Import/price-table/export",
            importBody!.Data!.Rows,
            JsonTestHelper.Options);

        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            exportResponse.Content.Headers.ContentType?.MediaType);

        var bytes = await exportResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal(0x50, bytes[0]);
    }

    [Fact]
    public async Task ExportPriceTableResult_EmptyBody_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Import/price-table/export",
            new List<PriceTableRow>());

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
