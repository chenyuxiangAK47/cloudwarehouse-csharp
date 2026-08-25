using System.Net;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.IntegrationTests;

public class SiteAndStaticApiTests : IClassFixture<CloudWarehouseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SiteAndStaticApiTests(CloudWarehouseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSites_ReturnsApiEnvelope()
    {
        var response = await _client.GetAsync("/api/Site");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<IEnumerable<Site>>>(response);
        Assert.NotNull(body);
        if (!body.Success)
            Assert.False(string.IsNullOrWhiteSpace(body.Message));
    }

    [Fact]
    public async Task GetDestinations_ReturnsApiEnvelope()
    {
        var response = await _client.GetAsync("/api/Destination");
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<IEnumerable<Destination>>>(response);

        Assert.NotNull(body);
    }

    [Fact]
    public async Task DownloadSiteImportTemplate_ReturnsXlsx()
    {
        var response = await _client.GetAsync("/api/Site/import/template");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal(0x50, bytes[0]);
    }

    [Fact]
    public async Task DownloadDestinationImportTemplate_ReturnsXlsx()
    {
        var response = await _client.GetAsync("/api/Destination/import/template");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
    }

    [Fact]
    public async Task GetCustomers_ReturnsApiEnvelope()
    {
        var response = await _client.GetAsync("/api/Customer");
        var body = await JsonTestHelper.ReadApiAsync<ApiResponse<IEnumerable<Customer>>>(response);
        Assert.NotNull(body);
    }

    [Fact]
    public async Task IndexHtml_IsServed()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Cloud Warehouse Management", html);
        Assert.Contains("Customer Management", html);
    }
}
