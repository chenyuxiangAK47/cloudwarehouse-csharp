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
        DateTime orderDate) =>
        DefaultEngine.Calculate(rules, weight, orderDate);

    public static PriceCalculateResult? CalculateActive(IList<PriceRule> rules, decimal weight) =>
        DefaultEngine.CalculateActive(rules, weight);
}
