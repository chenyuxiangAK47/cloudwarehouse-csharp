using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Tests;

public class PriceRuleMapperTests
{
    [Fact]
    public void ToPriceRules_CreatesTierRulesAndOverweightRule()
    {
        var row = new PriceTableRow
        {
            RowNumber = 2,
            EffectiveDate = new DateTime(2026, 5, 7),
            Price_0_5_1 = 2.1m,
            AdditionalUnitPrice = 0.7m,
            BaseFee = 3.5m
        };

        var rules = PriceRuleMapper.ToPriceRules(row, 1, 2);

        Assert.Contains(rules, r => r.BillingType == 1 && r.MaxWeight == 1m && r.UnitPrice == 2.1m);
        Assert.Contains(rules, r => r.BillingType == 2 && r.UnitPrice == 0.7m && r.MinWeight == 5m);
    }
}
