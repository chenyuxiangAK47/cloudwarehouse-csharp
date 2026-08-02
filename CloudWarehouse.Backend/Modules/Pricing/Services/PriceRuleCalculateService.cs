using CloudWarehouse.Backend.Helpers.Billing;
using CloudWarehouse.Backend.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Services;

public class PriceRuleCalculateService
{
    private readonly string _conn;
    private readonly FeeCalculationEngine _feeEngine;

    public PriceRuleCalculateService(IConfiguration config, FeeCalculationEngine feeEngine)
    {
        _conn = config.GetConnectionString("DefaultConnection")!;
        _feeEngine = feeEngine;
    }

    public async Task<PriceCalculateResult?> CalculateAsync(PriceCalculateRequest request)
    {
        if (request.Weight <= 0) return null;

        var orderDate = request.OrderDate?.Date ?? DateTime.Today;

        using var db = new SqlConnection(_conn);
        var rules = (await db.QueryAsync<PriceRule>(@"
            SELECT * FROM PriceRules
            WHERE SiteId = @SiteId AND DestId = @DestId AND Status = 1
              AND EffectiveDate <= @OrderDate
              AND (ExpiryDate IS NULL OR ExpiryDate >= @OrderDate)
            ORDER BY BillingType, MinWeight",
            new { request.SiteId, request.DestId, OrderDate = orderDate })).ToList();

        if (rules.Count == 0) return null;

        return _feeEngine.Calculate(rules, request.Weight, orderDate);
    }
}
