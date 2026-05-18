using CloudWarehouse.Backend.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Services;

public class PriceRuleCalculateService
{
    private readonly string _conn;

    public PriceRuleCalculateService(IConfiguration config)
    {
        _conn = config.GetConnectionString("DefaultConnection")!;
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

        if (request.Weight <= 5m)
        {
            var tier = rules.FirstOrDefault(r =>
                r.BillingType == 1 && request.Weight > r.MinWeight && request.Weight <= r.MaxWeight);
            if (tier == null) return null;

            var weightFee = tier.UnitPrice;
            return new PriceCalculateResult
            {
                BillingType = "区间计费(≤5kg)",
                BaseFee = tier.BaseFee,
                WeightFee = weightFee,
                TotalPrice = Math.Round(weightFee + tier.BaseFee, 2)
            };
        }

        var over = rules.FirstOrDefault(r => r.BillingType == 2);
        if (over == null) return null;

        var extra = (request.Weight - 5m) * over.UnitPrice;
        return new PriceCalculateResult
        {
            BillingType = "续重计费(>5kg)",
            BaseFee = over.BaseFee,
            WeightFee = Math.Round(extra, 2),
            TotalPrice = Math.Round(over.BaseFee + extra, 2)
        };
    }
}
