using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers.Billing;

/// <summary>
/// 体积重计费：当 L×W×H/divisor 大于实际重量时，按体积重走区间/续重算法。
/// 用于证明 Strategy 开闭扩展（无需改 FeeRuleCalculator 调用方）。
/// </summary>
public sealed class VolumetricBillingStrategy : IBillingStrategy
{
    private readonly TierBillingStrategy _tier = new();
    private readonly OverweightBillingStrategy _overweight = new();

    public string StrategyName => "体积重计费";

    public bool CanHandle(BillingContext context) =>
        context.HasVolumetricDimensions
        && context.VolumetricWeightKg is { } vol
        && vol > context.Weight
        && vol > 0;

    public PriceCalculateResult? Calculate(BillingContext context)
    {
        if (!CanHandle(context))
            return null;

        var chargeable = context.VolumetricWeightKg!.Value;
        var chargedCtx = new BillingContext
        {
            Weight = chargeable,
            ActiveRules = context.ActiveRules
        };

        var inner = chargeable <= 5m
            ? (IBillingStrategy)_tier
            : _overweight;

        var result = inner.Calculate(chargedCtx);
        if (result == null)
            return null;

        return new PriceCalculateResult
        {
            BillingType = $"{StrategyName}(计费重{chargeable:0.###}kg)",
            BaseFee = result.BaseFee,
            WeightFee = result.WeightFee,
            TotalPrice = result.TotalPrice
        };
    }
}
