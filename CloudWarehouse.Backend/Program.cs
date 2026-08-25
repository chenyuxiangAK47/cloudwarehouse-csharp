using System.Text;
using CloudWarehouse.Backend.Helpers.Billing;
using CloudWarehouse.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    // Prefer ASPNETCORE_URLS / --urls (CI DAST); else default LAN bind for local demo
    var configuredUrls = builder.Configuration["urls"]
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    if (string.IsNullOrWhiteSpace(configuredUrls))
        builder.WebHost.UseUrls("http://0.0.0.0:5001");
}

builder.Services.AddControllers();

// Billing Strategy Pattern — register concrete strategies + resolver + engine (DI)
// Order matters for IEnumerable<IBillingStrategy>: volumetric first, then tier, then overweight
builder.Services.AddSingleton<IBillingStrategy, VolumetricBillingStrategy>();
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

// Freight / quote assistant (RAG-lite) — assistive layer, not system of record
builder.Services.AddSingleton<IKnowledgeBaseLoader, KnowledgeBaseLoader>();
builder.Services.AddSingleton<IKeywordRetriever, KeywordRetriever>();
builder.Services.AddScoped<IQuoteAssistantService, QuoteAssistantService>();
builder.Services.AddHttpClient("QuoteAssistantLlm");

builder.Services.AddCors(p => p.AddPolicy("AllowAll", b =>
{
    b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
}));

var demoJwtEnabled = builder.Configuration.GetValue("Auth:DemoJwt:Enabled", false);
if (demoJwtEnabled)
{
    var signingKey = builder.Configuration["Auth:DemoJwt:SigningKey"]
        ?? "CloudWarehouse-Demo-Signing-Key-32chars!!";
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "CloudWarehouse.Demo",
                ValidAudience = "CloudWarehouse.Demo",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
            };
        });
}
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
if (demoJwtEnabled)
    app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
