using Microsoft.Playwright;

namespace CloudWarehouse.E2ETests;

public sealed class PlaywrightUiFixture : IAsyncLifetime, IDisposable
{
    private PlaywrightWebApplicationFactory? _factory;
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl { get; private set; } = "";

    public async Task InitializeAsync()
    {
        _factory = new PlaywrightWebApplicationFactory();
        _factory.CreateClient();

        if (string.IsNullOrWhiteSpace(_factory.ServerAddress))
            throw new InvalidOperationException("Kestrel server address was not initialized.");

        BaseUrl = _factory.ServerAddress.TrimEnd('/');

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
            await Browser.DisposeAsync();
        _playwright?.Dispose();
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }
}
