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
    public async Task IndexHtml_IsServed()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("云仓管理系统", html);
        Assert.Contains("价格表导入", html);
    }
}
