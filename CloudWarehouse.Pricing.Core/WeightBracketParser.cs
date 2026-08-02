namespace CloudWarehouse.Backend.Helpers;

/// <summary>师傅账单「公斤段」列 → 计费区间（与 PriceRule 区间一致）。</summary>
public static class WeightBracketParser
{
    public static (decimal Min, decimal Max, int BillingType)? Parse(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return null;

        var text = label.Trim().Replace("kg", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (text.StartsWith('>'))
            return (5m, 99999m, 2);

        if (!decimal.TryParse(text, out var upper))
            return null;

        return upper switch
        {
            0.3m => (0m, 0.3m, 1),
            0.5m => (0.3m, 0.5m, 1),
            1m => (0.5m, 1m, 1),
            2m => (1m, 2m, 1),
            3m => (2m, 3m, 1),
            4m => (3m, 4m, 1),
            5m => (4m, 5m, 1),
            _ => null
        };
    }
}
