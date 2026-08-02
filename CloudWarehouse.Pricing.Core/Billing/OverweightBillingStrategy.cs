using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers.Billing;

/// <summary>续重计费（&gt;5kg）：面单费 + (重量-5)×续重单价，BillingType=2。</summary>
public sealed class OverweightBillingStrategy : IBillingStrategy
{
    public string StrategyName => "续重计费(>5kg)";

    public bool CanHandle(BillingContext context) => context.Weight > 5m;

    public PriceCalculateResult? Calculate(BillingContext context)
    {
        if (!CanHandle(context))
            return null;

        var over = context.ActiveRules.FirstOrDefault(r => r.BillingType == 2);
        if (over == null)
            return null;

        var extra = (context.Weight - 5m) * over.UnitPrice;
        return new PriceCalculateResult
        {
            BillingType = StrategyName,
            BaseFee = over.BaseFee,
            WeightFee = Math.Round(extra, 2),
            TotalPrice = Math.Round(over.BaseFee + extra, 2)
        };
    }
}
