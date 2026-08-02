namespace CloudWarehouse.Backend.Helpers;

/// <summary>
/// 计费重量取整（与师傅 Excel 账单模版 K 列公式一致）。
/// 正向计费：≤0.3→0.3，≤0.5→0.5，≤5→向上取整公斤，>5→返回 null（走续重逻辑，由调用方处理）。
/// </summary>
public static class WeightRounding
{
    public const string Over5Marker = ">5";

    public static string? RoundForForwardBilling(decimal weight)
    {
        if (weight <= 0) return null;
        if (weight <= 0.3m) return "0.3";
        if (weight <= 0.5m) return "0.5";
        if (weight <= 5m) return Math.Ceiling(weight).ToString("0.##");
        return Over5Marker;
    }

    /// <summary>取整后的数值重量；&gt;5kg 时返回原重量。</summary>
    public static decimal? RoundWeight(decimal weight)
    {
        var label = RoundForForwardBilling(weight);
        if (label == null) return null;
        if (label == Over5Marker) return weight;
        return decimal.Parse(label);
    }
}
