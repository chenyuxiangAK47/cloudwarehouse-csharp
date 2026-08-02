using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers.Billing;

/// <summary>区间计费（≤5kg）：匹配 BillingType=1 的重量档单价 + 面单费。</summary>
public sealed class TierBillingStrategy : IBillingStrategy
{
    public string StrategyName => "区间计费(≤5kg)";

    public bool CanHandle(BillingContext context) =>
        context.Weight > 0 && context.Weight <= 5m;

    public PriceCalculateResult? Calculate(BillingContext context)
    {
        if (!CanHandle(context))
            return null;

        var tier = context.ActiveRules.FirstOrDefault(r =>
            r.BillingType == 1
            && context.Weight > r.MinWeight
            && context.Weight <= r.MaxWeight);

        if (tier == null)
            return null;

        return new PriceCalculateResult
        {
            BillingType = StrategyName,
            BaseFee = tier.BaseFee,
            WeightFee = tier.UnitPrice,
            TotalPrice = Math.Round(tier.UnitPrice + tier.BaseFee, 2)
        };
    }
}
