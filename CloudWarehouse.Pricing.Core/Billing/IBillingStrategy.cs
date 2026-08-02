using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers.Billing;

/// <summary>计费策略接口（Strategy Pattern）— 每种计费变体一个实现。</summary>
public interface IBillingStrategy
{
    string StrategyName { get; }

    /// <summary>当前策略是否适用于该重量。</summary>
    bool CanHandle(BillingContext context);

    PriceCalculateResult? Calculate(BillingContext context);
}
