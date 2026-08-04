using CloudWarehouse.Backend.Helpers.Billing;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers;

/// <summary>
/// 静态门面：委托 <see cref="FeeCalculationEngine"/> + 默认策略解析器。
/// 新代码可注入 <see cref="FeeCalculationEngine"/> 以便单测替换策略。
/// </summary>
public static class FeeRuleCalculator
{
    private static readonly FeeCalculationEngine DefaultEngine =
        new(DefaultBillingStrategyResolver.CreateDefault());

    public static PriceCalculateResult? Calculate(
        IEnumerable<PriceRule> rules,
        decimal weight,
        DateTime orderDate,
        decimal? lengthCm = null,
        decimal? widthCm = null,
        decimal? heightCm = null,
        decimal volumetricDivisor = 6000m) =>
        DefaultEngine.Calculate(rules, weight, orderDate, lengthCm, widthCm, heightCm, volumetricDivisor);

    public static PriceCalculateResult? CalculateActive(
        IList<PriceRule> rules,
        decimal weight,
        decimal? lengthCm = null,
        decimal? widthCm = null,
        decimal? heightCm = null,
        decimal volumetricDivisor = 6000m) =>
        DefaultEngine.CalculateActive(rules, weight, lengthCm, widthCm, heightCm, volumetricDivisor);
}
