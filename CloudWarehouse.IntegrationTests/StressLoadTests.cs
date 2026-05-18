using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using CloudWarehouse.TestCommon;

namespace CloudWarehouse.IntegrationTests;

/// <summary>轻量压力/并发测试（随 dotnet test 运行，无需外网工具）。</summary>
[Trait("Category", "Stress")]
public class StressLoadTests : IClassFixture<CloudWarehouseWebApplicationFactory>
{
    private readonly CloudWarehouseWebApplicationFactory _factory;

    public StressLoadTests(CloudWarehouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TemplateDownload_30Concurrent_AllSucceed_Under10Seconds()
    {
        const int concurrent = 30;
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, concurrent).Select(_ =>
        {
            var client = _factory.CreateClient();
            return client.GetAsync("/api/Import/price-table/template");
        });

        var responses = await Task.WhenAll(tasks);
        sw.Stop();

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10),
            $"30 次并发下载模板耗时 {sw.Elapsed.TotalSeconds:F1}s，超过 10s 阈值");
    }

    [Fact]
    public async Task PriceTablePreview_15Concurrent_AllReturnSuccess()
    {
        const int concurrent = 15;
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, concurrent).Select(async _ =>
        {
            using var stream = PriceTableExcelFactory.CreateValidWorkbook();
            using var content = BuildMultipart(stream);
            var client = _factory.CreateClient();
            return await client.PostAsync("/api/Import/price-table/preview", content);
        });

        var responses = await Task.WhenAll(tasks);
        sw.Stop();

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30),
            $"15 次并发预览耗时 {sw.Elapsed.TotalSeconds:F1}s，超过 30s 阈值");
    }

    private static MultipartFormDataContent BuildMultipart(Stream stream)
    {
        var content = new MultipartFormDataContent();
        var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(file, "file", "stress.xlsx");
        return content;
    }
}
