using CloudWarehouse.Backend.Helpers.Billing;
using CloudWarehouse.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
    // 0.0.0.0 = 允许局域网/远程用 http://本机IP:5001 访问；仅本机可用 http://localhost:5001
    builder.WebHost.UseUrls("http://0.0.0.0:5001");

builder.Services.AddControllers();

// Billing Strategy Pattern — register concrete strategies + resolver + engine (DI)
builder.Services.AddSingleton<IBillingStrategy, TierBillingStrategy>();
builder.Services.AddSingleton<IBillingStrategy, OverweightBillingStrategy>();
builder.Services.AddSingleton<IBillingStrategyResolver, DefaultBillingStrategyResolver>();
builder.Services.AddSingleton<FeeCalculationEngine>();

builder.Services.AddScoped<PriceRuleImportService>();
builder.Services.AddScoped<PriceRuleCalculateService>();
builder.Services.AddScoped<CustomerQuoteImportService>();
builder.Services.AddScoped<CustomerQuoteCalculateService>();
builder.Services.AddScoped<IDualTrackFeeCalculator, DualTrackFeeCalculator>();
builder.Services.AddScoped<BillImportService>();
builder.Services.AddCors(p => p.AddPolicy("AllowAll", b =>
{
    b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
}));

var app = builder.Build();

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
