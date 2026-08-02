using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://localhost:5002");

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "CloudWarehouse.Pricing.Api",
    purpose = "NUS academic demo — extractable pricing microservice",
    endpoint = "POST /api/calculate/preview"
}));

app.MapPost("/api/calculate/preview", (CalculatePreviewRequest request) =>
{
    if (request.Weight <= 0)
        return Results.BadRequest(new { error = "Weight must be positive." });

    var row = request.Row ?? new PriceTableRow();
    var total = PriceCalculator.Calculate(row, request.Weight);
    if (total == null)
        return Results.Ok(new { request.Weight, totalPrice = (decimal?)null, message = "No applicable tier or rate." });

    return Results.Ok(new { request.Weight, totalPrice = total });
});

app.Run();

public class CalculatePreviewRequest
{
    public decimal Weight { get; set; }
    public PriceTableRow? Row { get; set; }
}

public partial class Program;
