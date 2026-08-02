using CloudWarehouse.Backend.Helpers;

namespace CloudWarehouse.Tests;

public class MasterCostExcelTests
{
    [Fact]
    public void ReadPriceTable_93CostSheet_ParsesLongFormat()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "excel",
            "93-成本表-单独.xlsx"));
        if (!File.Exists(path))
            return;

        using var stream = File.OpenRead(path);
        var result = ExcelHelper.ReadPriceTable(stream);

        Assert.Equal("师傅成本表(地区+重量段)", result.Format);
        Assert.True(result.TotalRows > 100);

        var jinzhouYunnan = result.Rows
            .Where(r => r.SiteCode == "中通-晋州" && r.Destination == "云南省")
            .ToList();
        Assert.NotEmpty(jinzhouYunnan);

        var jan2026Period = jinzhouYunnan.FirstOrDefault(r =>
            r.EffectiveDate <= new DateTime(2026, 1, 9)
            && (r.ExpiryDate == null || r.ExpiryDate >= new DateTime(2026, 1, 9)));
        Assert.NotNull(jan2026Period);
        Assert.Equal(1.5m, jan2026Period!.Price_0_0_3);
        Assert.Equal(1.75m, jan2026Period.Price_0_3_0_5);

        var latestPeriod = jinzhouYunnan.OrderByDescending(r => r.EffectiveDate).First();
        Assert.Equal(1.3m, latestPeriod.Price_0_0_3);
    }

    [Fact]
    public void ReadPriceTable_93CostSheet_Jan2026Yunnan03kg_UsesHistoricalPayablePrice()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "excel",
            "93-成本表-单独.xlsx"));
        if (!File.Exists(path))
            return;

        using var stream = File.OpenRead(path);
        var parsed = ExcelHelper.ReadPriceTable(stream);
        var janPeriod = parsed.Rows.First(r =>
            r.SiteCode == "中通-晋州"
            && r.Destination == "云南省"
            && r.EffectiveDate <= new DateTime(2026, 1, 9)
            && (r.ExpiryDate == null || r.ExpiryDate >= new DateTime(2026, 1, 9)));

        var rules = PriceRuleMapper.ToPriceRules(janPeriod, 1, 1);
        var payable = FeeRuleCalculator.Calculate(rules, 0.3m, new DateTime(2026, 1, 9));

        Assert.NotNull(payable);
        Assert.Equal(1.5m, payable!.WeightFee);
    }
}
