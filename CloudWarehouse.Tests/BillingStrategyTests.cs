using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Helpers.Billing;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Tests;

public class BillingStrategyTests
{
    private static List<PriceRule> SampleRules() =>
    [
        new()
        {
            BillingType = 1, MinWeight = 0m, MaxWeight = 0.3m, UnitPrice = 1.5m, BaseFee = 0m,
            Status = 1, EffectiveDate = new DateTime(2025, 11, 1)
        },
        new()
        {
            BillingType = 1, MinWeight = 0.3m, MaxWeight = 0.5m, UnitPrice = 1.75m, BaseFee = 0m,
            Status = 1, EffectiveDate = new DateTime(2025, 11, 1)
        },
        new()
        {
            BillingType = 1, MinWeight = 4m, MaxWeight = 5m, UnitPrice = 6m, BaseFee = 3.5m,
            Status = 1, EffectiveDate = new DateTime(2025, 11, 1)
        },
        new()
        {
            BillingType = 2, MinWeight = 5m, MaxWeight = 99999m, UnitPrice = 0.7m, BaseFee = 3.5m,
            Status = 1, EffectiveDate = new DateTime(2025, 11, 1)
        }
    ];

    [Fact]
    public void Factory_ResolvesTierStrategy_ForWeightWithin5Kg()
    {
        var ctx = new BillingContext { Weight = 0.3m, ActiveRules = SampleRules() };
        var strategy = BillingStrategyFactory.Resolve(ctx);
        Assert.IsType<TierBillingStrategy>(strategy);
    }

    [Fact]
    public void Factory_ResolvesOverweightStrategy_ForWeightAbove5Kg()
    {
        var ctx = new BillingContext { Weight = 10m, ActiveRules = SampleRules() };
        var strategy = BillingStrategyFactory.Resolve(ctx);
        Assert.IsType<OverweightBillingStrategy>(strategy);
    }

    [Fact]
    public void TierStrategy_YunNan03kg_MatchesHistoricalPayableShape()
    {
        var rules = SampleRules();
        var result = FeeRuleCalculator.Calculate(rules, 0.3m, new DateTime(2026, 1, 9));
        Assert.NotNull(result);
        Assert.Equal("区间计费(≤5kg)", result!.BillingType);
        Assert.Equal(1.5m, result.WeightFee);
        Assert.Equal(1.5m, result.TotalPrice);
    }

    [Fact]
    public void OverweightStrategy_10Kg_UsesBasePlusExtra()
    {
        var rules = SampleRules();
        var result = FeeRuleCalculator.Calculate(rules, 10m, new DateTime(2026, 1, 9));
        Assert.NotNull(result);
        Assert.Equal("续重计费(>5kg)", result!.BillingType);
        // 3.5 + (10-5)*0.7 = 7.0
        Assert.Equal(7.0m, result.TotalPrice);
    }

    [Fact]
    public void CalculateActive_BehaviorUnchanged_ViaStrategyDelegation()
    {
        var rules = SampleRules();
        var viaFacade = FeeRuleCalculator.CalculateActive(rules, 5m);
        var strategy = new TierBillingStrategy();
        var viaStrategy = strategy.Calculate(new BillingContext { Weight = 5m, ActiveRules = rules });

        Assert.NotNull(viaFacade);
        Assert.NotNull(viaStrategy);
        Assert.Equal(viaFacade!.TotalPrice, viaStrategy!.TotalPrice);
        Assert.Equal(viaFacade.BillingType, viaStrategy.BillingType);
    }

    [Fact]
    public void RegisteredStrategyNames_ListsCurrentImplementations()
    {
        var names = BillingStrategyFactory.RegisteredStrategyNames;
        Assert.Contains("区间计费(≤5kg)", names);
        Assert.Contains("续重计费(>5kg)", names);
        Assert.Equal(2, names.Count);
    }

    [Fact]
    public void FeeCalculationEngine_UsesInjectedResolver()
    {
        var engine = new FeeCalculationEngine(DefaultBillingStrategyResolver.CreateDefault());
        var result = engine.Calculate(SampleRules(), 0.3m, new DateTime(2026, 1, 9));
        Assert.NotNull(result);
        Assert.Equal(1.5m, result!.TotalPrice);
    }
}
