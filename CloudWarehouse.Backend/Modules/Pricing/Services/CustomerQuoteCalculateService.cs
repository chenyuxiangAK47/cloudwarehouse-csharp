using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Helpers.Billing;
using CloudWarehouse.Backend.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Services;

public class CustomerQuoteCalculateService
{
    private readonly string _conn;
    private readonly FeeCalculationEngine _feeEngine;

    public CustomerQuoteCalculateService(IConfiguration config, FeeCalculationEngine feeEngine)
    {
        _conn = config.GetConnectionString("DefaultConnection")!;
        _feeEngine = feeEngine;
    }

    public async Task<PriceCalculateResult?> CalculateAsync(CustomerQuoteCalculateRequest request)
    {
        if (request.Weight <= 0)
            return null;

        using var db = new SqlConnection(_conn);
        var rules = (await db.QueryAsync<CustomerQuoteRule>(@"
            SELECT * FROM CustomerQuoteRules
            WHERE CustomerId = @CustomerId AND Status = 1
              AND EffectiveDate <= @OrderDate
              AND (ExpiryDate IS NULL OR ExpiryDate >= @OrderDate)",
            new
            {
                request.CustomerId,
                OrderDate = request.OrderDate?.Date ?? DateTime.Today
            })).ToList();

        return CalculateFromRules(rules, request.Province, request.ExpressType, request.Weight,
            request.OrderDate?.Date ?? DateTime.Today, _feeEngine);
    }

    public static PriceCalculateResult? CalculateFromRules(
        IList<CustomerQuoteRule> rules,
        string province,
        string? expressType,
        decimal weight,
        DateTime orderDate) =>
        CalculateFromRules(rules, province, expressType, weight, orderDate,
            new FeeCalculationEngine(DefaultBillingStrategyResolver.CreateDefault()));

    public static PriceCalculateResult? CalculateFromRules(
        IList<CustomerQuoteRule> rules,
        string province,
        string? expressType,
        decimal weight,
        DateTime orderDate,
        FeeCalculationEngine feeEngine)
    {
        var normalized = BillImportService.NormalizeRegion(province);
        var matched = rules
            .Where(r => BillImportService.NormalizeRegion(r.Province)
                .Equals(normalized, StringComparison.OrdinalIgnoreCase))
            .Where(r => ExpressMatches(r.ExpressType, expressType))
            .ToList();

        if (matched.Count == 0)
            return null;

        var latestEffective = matched.Max(r => r.EffectiveDate.Date);
        var periodRules = matched.Where(r => r.EffectiveDate.Date == latestEffective).ToList();
        var priceRules = periodRules.Select(CustomerQuoteRuleMapper.ToPriceRule).ToList();
        return feeEngine.Calculate(priceRules, weight, orderDate);
    }

    private static bool ExpressMatches(string? ruleExpress, string? requestExpress)
    {
        if (string.IsNullOrWhiteSpace(ruleExpress))
            return true;

        if (string.IsNullOrWhiteSpace(requestExpress))
            return true;

        return ruleExpress.Equals(requestExpress, StringComparison.OrdinalIgnoreCase)
            || requestExpress.Contains(ruleExpress, StringComparison.OrdinalIgnoreCase)
            || ruleExpress.Contains(requestExpress, StringComparison.OrdinalIgnoreCase);
    }
}
