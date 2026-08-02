using System.Net;
using System.Net.Http.Headers;
using CloudWarehouse.Backend.Models;
using CloudWarehouse.TestCommon;

namespace CloudWarehouse.IntegrationTests;

public class CustomerQuoteApiTests : IClassFixture<CloudWarehouseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CustomerQuoteApiTests(CloudWarehouseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DownloadTemplate_ReturnsXlsx()
    {
        var response = await _client.GetAsync("/api/CustomerQuote/template");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
    }

    [Fact]
    public async Task PreviewCustomerQuote_StandardFormat_ParsesRow()
    {
        using var stream = CustomerQuoteExcelFactory.CreateStandardWideWorkbook();
        using var content = BuildFileContent(stream, "客户报价.xlsx");

        var response = await _client.PostAsync("/api/CustomerQuote/preview", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<CustomerQuoteImportResult>>(response);

        Assert.True(body!.Success, body.Message);
        Assert.Single(body.Data!.WideRows);
        Assert.Equal(9.9m, body.Data.WideRows[0].Price_2_3);
    }

    [Fact]
    public async Task ImportCustomerQuote_WhenDatabaseAvailable_SavesRules()
    {
        using var stream = CustomerQuoteExcelFactory.CreateStandardWideWorkbook(
            province: "集成测试省_" + Guid.NewGuid().ToString("N")[..6],
            tier2to3: 11m);
        using var content = BuildFileContent(stream, "客户报价.xlsx");

        var response = await _client.PostAsync("/api/CustomerQuote", content);
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<CustomerQuoteImportResult>>(response);

        if (body!.Data?.Warnings.Any(w => w.Contains("未能连接数据库", StringComparison.Ordinal)) == true)
            return;

        Assert.True(body.Success, body.Message);
        Assert.True(body.Data!.SavedToDatabase);
        Assert.True(body.Data.RulesUpserted > 0);
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
