// Report §8.3.2 — Playwright UI smoke E2E (Category=E2E).
// Verifies index.html primary nav + Waybill / Customer Quote / Rule RAG panels.
// CI: install Chromium then `dotnet test CloudWarehouse.E2ETests`.
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace CloudWarehouse.E2ETests;

[Trait("Category", "E2E")]
public class UiSmokeE2ETests : IClassFixture<PlaywrightUiFixture>, IAsyncLifetime
{
    private readonly PlaywrightUiFixture _fixture;
    private IBrowserContext? _context;
    private IPage? _page;

    public UiSmokeE2ETests(PlaywrightUiFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context is not null)
            await _context.DisposeAsync();
    }

    [Fact]
    public async Task Home_LoadsTitleAndPrimaryNav()
    {
        await _page!.GotoAsync(_fixture.BaseUrl + "/");

        await Assertions.Expect(_page).ToHaveTitleAsync(new Regex("Cloud Warehouse", RegexOptions.IgnoreCase));
        await Assertions.Expect(_page.Locator("h1")).ToContainTextAsync("Cloud Warehouse Management");
        await Assertions.Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Waybill Import" })).ToBeVisibleAsync();
        await Assertions.Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Customer Quote Import" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Nav_WaybillImport_ShowsUploadSection()
    {
        await _page!.GotoAsync(_fixture.BaseUrl + "/");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Waybill Import" }).ClickAsync();

        await Assertions.Expect(_page.Locator("#waybill")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("#waybill h2")).ToHaveTextAsync("Waybill Import");
        await Assertions.Expect(_page.Locator("#waybill input[type='file']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Nav_CustomerQuoteImport_ShowsUploadSection()
    {
        await _page!.GotoAsync(_fixture.BaseUrl + "/");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Customer Quote Import" }).ClickAsync();

        await Assertions.Expect(_page.Locator("#customerquote")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("#customerquote h2")).ToHaveTextAsync("Customer Quote Import");
    }

    [Fact]
    public async Task Nav_RuleRag_ShowsAssistantPanel()
    {
        await _page!.GotoAsync(_fixture.BaseUrl + "/");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Rule RAG" }).ClickAsync();

        await Assertions.Expect(_page.Locator("#assistant")).ToBeVisibleAsync();
        await Assertions.Expect(_page.Locator("#assistant")).ToContainTextAsync("Rule");
    }
}
