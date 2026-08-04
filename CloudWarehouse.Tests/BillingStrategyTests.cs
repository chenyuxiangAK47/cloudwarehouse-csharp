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
    public void Factory_ResolvesVolumetric_WhenVolWeightExceedsPhysical()
    {
        // 40×40×40 / 6000 = 10.667kg > physical 2kg
        var ctx = new BillingContext
        {
            Weight = 2m,
            ActiveRules = SampleRules(),
            LengthCm = 40, WidthCm = 40, HeightCm = 40
        };
        var strategy = BillingStrategyFactory.Resolve(ctx);
        Assert.IsType<VolumetricBillingStrategy>(strategy);
    }

    [Fact]
    public void VolumetricStrategy_UsesChargeableWeight_ForOverweightBand()
    {
        var rules = SampleRules();
        var result = FeeRuleCalculator.Calculate(
            rules, 2m, new DateTime(2026, 1, 9),
            lengthCm: 40, widthCm: 40, heightCm: 40);

        Assert.NotNull(result);
        Assert.StartsWith("体积重计费", result!.BillingType);
        // chargeable ≈ 10.667 → 3.5 + (10.667-5)*0.7 ≈ 7.467
        Assert.Equal(7.47m, result.TotalPrice);
    }

    [Fact]
    public void VolumetricStrategy_DoesNotOverride_WhenPhysicalHeavier()
    {
        // 10×10×10 / 6000 ≈ 0.167 < physical 0.3 → stay on Tier
        var ctx = new BillingContext
        {
            Weight = 0.3m,
            ActiveRules = SampleRules(),
            LengthCm = 10, WidthCm = 10, HeightCm = 10
        };
        Assert.IsType<TierBillingStrategy>(BillingStrategyFactory.Resolve(ctx));
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
        Assert.Contains("体积重计费", names);
        Assert.Equal(3, names.Count);
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
