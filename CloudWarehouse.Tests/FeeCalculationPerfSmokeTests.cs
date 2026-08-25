using System.Diagnostics;
using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Tests;

/// <summary>
/// Lightweight local baseline (not a load-test suite). Numbers can be pasted into the report appendix.
/// </summary>
public class FeeCalculationPerfSmokeTests
{
    private static List<PriceRule> Rules() =>
    [
        new()
        {
            BillingType = 1, MinWeight = 0m, MaxWeight = 0.3m, UnitPrice = 1.5m, BaseFee = 0m,
            Status = 1, EffectiveDate = new DateTime(2025, 11, 1)
        },
        new()
        {
            BillingType = 2, MinWeight = 5m, MaxWeight = 99999m, UnitPrice = 0.7m, BaseFee = 3.5m,
            Status = 1, EffectiveDate = new DateTime(2025, 11, 1)
        }
    ];

    [Fact]
    public void CalculateActive_1000Iterations_CompletesUnder200Ms()
    {
        var rules = Rules();
        // Warmup
        _ = FeeRuleCalculator.CalculateActive(rules, 0.3m);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 1000; i++)
            _ = FeeRuleCalculator.CalculateActive(rules, i % 2 == 0 ? 0.3m : 10m);
        sw.Stop();

        // Generous bound for CI runners; local machines are typically far faster.
        Assert.True(sw.ElapsedMilliseconds < 200,
            $"1000 CalculateActive calls took {sw.ElapsedMilliseconds} ms");

        Console.WriteLine(
            $"[PERF] FeeRuleCalculator.CalculateActive x1000: {sw.ElapsedMilliseconds} ms");
    }
}
