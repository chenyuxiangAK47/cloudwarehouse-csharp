using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CloudWarehouse.E2ETests;

/// <summary>
/// Starts Kestrel on a dynamic localhost port so Playwright can drive a real browser.
/// </summary>
public sealed class PlaywrightWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _kestrelHost;

    public string ServerAddress { get; private set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls("http://127.0.0.1:0");
        });

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var server = _kestrelHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        ServerAddress = addresses!.Addresses.First(a =>
            a.StartsWith("http://", StringComparison.OrdinalIgnoreCase));

        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _kestrelHost?.Dispose();
        base.Dispose(disposing);
    }
}
