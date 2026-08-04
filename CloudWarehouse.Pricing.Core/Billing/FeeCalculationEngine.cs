using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers.Billing;

/// <summary>
/// 可注入的计费引擎：过滤生效规则 → 解析策略 → 计算。
/// 静态门面 <see cref="FeeRuleCalculator"/> 委托到默认实例，保持旧调用兼容。
/// </summary>
public sealed class FeeCalculationEngine
{
    private readonly IBillingStrategyResolver _resolver;

    public FeeCalculationEngine(IBillingStrategyResolver resolver)
    {
        _resolver = resolver;
    }

    public PriceCalculateResult? Calculate(
        IEnumerable<PriceRule> rules,
        decimal weight,
        DateTime orderDate,
        decimal? lengthCm = null,
        decimal? widthCm = null,
        decimal? heightCm = null,
        decimal volumetricDivisor = 6000m)
    {
        var active = rules
            .Where(r => r.Status == 1
                && r.EffectiveDate.Date <= orderDate.Date
                && (r.ExpiryDate == null || r.ExpiryDate.Value.Date >= orderDate.Date))
            .OrderBy(r => r.BillingType)
            .ThenBy(r => r.MinWeight)
            .ToList();

        return CalculateActive(active, weight, lengthCm, widthCm, heightCm, volumetricDivisor);
    }

    public PriceCalculateResult? CalculateActive(
        IList<PriceRule> rules,
        decimal weight,
        decimal? lengthCm = null,
        decimal? widthCm = null,
        decimal? heightCm = null,
        decimal volumetricDivisor = 6000m)
    {
        if (weight <= 0 || rules.Count == 0)
            return null;

        var context = new BillingContext
        {
            Weight = weight,
            ActiveRules = rules is List<PriceRule> list ? list : rules.ToList(),
            LengthCm = lengthCm,
            WidthCm = widthCm,
            HeightCm = heightCm,
            VolumetricDivisor = volumetricDivisor
        };

        return _resolver.Resolve(context)?.Calculate(context);
    }
}
