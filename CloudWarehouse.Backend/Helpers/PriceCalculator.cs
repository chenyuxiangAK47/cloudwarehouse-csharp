using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers;

public static class PriceCalculator
{
    /// <summary>
    /// 根据导入行的区间价/面单费/续重单价计算预期运费。
    /// ≤5kg：匹配重量区间单价 + 面单费；>5kg：面单费 + (重量-5)×续重单价。
    /// </summary>
    public static decimal? Calculate(PriceTableRow row, decimal weight)
    {
        if (weight <= 0) return null;

        if (weight <= 5m)
        {
            var tierPrice = GetTierPrice(row, weight);
            if (tierPrice == null) return null;
            return Math.Round(tierPrice.Value + row.BaseFee, 2);
        }

        if (row.AdditionalUnitPrice <= 0) return null;
        return Math.Round(row.BaseFee + (weight - 5m) * row.AdditionalUnitPrice, 2);
    }

    private static decimal? GetTierPrice(PriceTableRow row, decimal weight)
    {
        if (weight <= 0.3m) return row.Price_0_0_3;
        if (weight <= 0.5m) return row.Price_0_3_0_5;
        if (weight <= 1m) return row.Price_0_5_1;
        if (weight <= 2m) return row.Price_1_2;
        if (weight <= 3m) return row.Price_2_3;
        if (weight <= 4m) return row.Price_3_4;
        if (weight <= 5m) return row.Price_4_5;
        return null;
    }
}
