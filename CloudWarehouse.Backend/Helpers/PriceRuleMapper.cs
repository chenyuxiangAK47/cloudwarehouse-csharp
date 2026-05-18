using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers;

public static class PriceRuleMapper
{
    private static readonly (decimal Min, decimal Max, Func<PriceTableRow, decimal?> Price)[] Tiers =
    [
        (0m, 0.3m, r => r.Price_0_0_3),
        (0.3m, 0.5m, r => r.Price_0_3_0_5),
        (0.5m, 1m, r => r.Price_0_5_1),
        (1m, 2m, r => r.Price_1_2),
        (2m, 3m, r => r.Price_2_3),
        (3m, 4m, r => r.Price_3_4),
        (4m, 5m, r => r.Price_4_5),
    ];

    public static List<PriceRule> ToPriceRules(PriceTableRow row, long siteId, long destId)
    {
        var effective = row.EffectiveDate?.Date ?? DateTime.Today;
        var rules = new List<PriceRule>();

        foreach (var (min, max, getPrice) in Tiers)
        {
            var price = getPrice(row);
            if (price == null) continue;

            rules.Add(new PriceRule
            {
                SiteId = siteId,
                DestId = destId,
                BillingType = 1,
                MinWeight = min,
                MaxWeight = max,
                UnitPrice = price.Value,
                BaseFee = row.BaseFee,
                EffectiveDate = effective,
                Status = 1,
                CreateTime = DateTime.Now,
                Remark = $"导入行{row.RowNumber}"
            });
        }

        if (row.AdditionalUnitPrice > 0)
        {
            rules.Add(new PriceRule
            {
                SiteId = siteId,
                DestId = destId,
                BillingType = 2,
                MinWeight = 5m,
                MaxWeight = 99999m,
                UnitPrice = row.AdditionalUnitPrice,
                BaseFee = row.BaseFee,
                EffectiveDate = effective,
                Status = 1,
                CreateTime = DateTime.Now,
                Remark = $"导入行{row.RowNumber}-续重"
            });
        }

        return rules;
    }
}
