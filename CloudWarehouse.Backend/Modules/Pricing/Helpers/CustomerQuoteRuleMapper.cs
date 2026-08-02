using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers;

public static class CustomerQuoteRuleMapper
{
    private static readonly (decimal Min, decimal Max, Func<CustomerQuoteTableRow, decimal?> Price)[] Tiers =
    [
        (0m, 0.3m, r => r.Price_0_0_3),
        (0.3m, 0.5m, r => r.Price_0_3_0_5),
        (0.5m, 1m, r => r.Price_0_5_1),
        (1m, 2m, r => r.Price_1_2),
        (2m, 3m, r => r.Price_2_3),
        (3m, 4m, r => r.Price_3_4),
        (4m, 5m, r => r.Price_4_5),
    ];

    public static List<CustomerQuoteRule> ToRules(CustomerQuoteTableRow row, long customerId)
    {
        var effective = row.EffectiveDate?.Date ?? DateTime.Today;
        var rules = new List<CustomerQuoteRule>();

        foreach (var (min, max, getPrice) in Tiers)
        {
            var price = getPrice(row);
            if (price == null)
                continue;

            rules.Add(CreateRule(customerId, row, effective, 1, min, max, price.Value, row.BaseFee,
                $"导入行{row.RowNumber}"));
        }

        if (row.AdditionalUnitPrice > 0)
        {
            rules.Add(CreateRule(customerId, row, effective, 2, 5m, 99999m, row.AdditionalUnitPrice, row.BaseFee,
                $"导入行{row.RowNumber}-续重"));
        }

        return rules;
    }

    public static CustomerQuoteRule? FromLongRow(CustomerQuoteLongRow row, long customerId)
    {
        var bracket = WeightBracketParser.Parse(row.WeightBracket);
        if (bracket == null)
            return null;

        var (min, max, billingType) = bracket.Value;
        return CreateRule(customerId, row.Province, row.ExpressType, row.EffectiveDate?.Date ?? DateTime.Today,
            billingType, min, max, row.UnitPrice, row.BaseFee, $"导入行{row.RowNumber}", row.ExpiryDate?.Date);
    }

    private static CustomerQuoteRule CreateRule(
        long customerId,
        CustomerQuoteTableRow row,
        DateTime effective,
        int billingType,
        decimal min,
        decimal max,
        decimal unitPrice,
        decimal baseFee,
        string remark) =>
        CreateRule(customerId, row.Province, row.ExpressType, effective, billingType, min, max, unitPrice, baseFee, remark);

    private static CustomerQuoteRule CreateRule(
        long customerId,
        string province,
        string? expressType,
        DateTime effective,
        int billingType,
        decimal min,
        decimal max,
        decimal unitPrice,
        decimal baseFee,
        string remark,
        DateTime? expiryDate = null) => new()
    {
        CustomerId = customerId,
        Province = province.Trim(),
        ExpressType = string.IsNullOrWhiteSpace(expressType) ? null : expressType.Trim(),
        BillingType = billingType,
        MinWeight = min,
        MaxWeight = max,
        UnitPrice = unitPrice,
        BaseFee = baseFee,
        EffectiveDate = effective,
        ExpiryDate = expiryDate,
        Status = 1,
        Remark = remark
    };

    public static PriceRule ToPriceRule(CustomerQuoteRule rule) => new()
    {
        BillingType = rule.BillingType,
        MinWeight = rule.MinWeight,
        MaxWeight = rule.MaxWeight,
        UnitPrice = rule.UnitPrice,
        BaseFee = rule.BaseFee,
        EffectiveDate = rule.EffectiveDate,
        ExpiryDate = rule.ExpiryDate,
        Status = rule.Status
    };
}
