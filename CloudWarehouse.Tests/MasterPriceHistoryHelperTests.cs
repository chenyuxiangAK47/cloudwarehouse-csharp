using CloudWarehouse.Backend.Helpers;

namespace CloudWarehouse.Tests;

public class MasterPriceHistoryHelperTests
{
    [Fact]
    public void PriceAtDate_PicksLatestVersionOnOrBeforeOrderDate()
    {
        var versions = new List<PriceHistoryVersion>
        {
            new(new DateTime(2025, 9, 5), 1.8m),
            new(new DateTime(2025, 11, 1), 1.5m),
            new(new DateTime(2026, 3, 23), 1.3m),
            new(new DateTime(2026, 4, 26), 1.3m)
        };

        Assert.Equal(1.5m, MasterPriceHistoryHelper.PriceAtDate(versions, new DateTime(2026, 1, 9)));
        Assert.Equal(1.3m, MasterPriceHistoryHelper.PriceAtDate(versions, new DateTime(2026, 4, 1)));
        Assert.Equal(1.8m, MasterPriceHistoryHelper.PriceAtDate(versions, new DateTime(2025, 10, 1)));
    }

    [Fact]
    public void BuildEffectivePeriods_SetsExpiryToDayBeforeNextVersion()
    {
        var periods = MasterPriceHistoryHelper.BuildEffectivePeriods([
            new DateTime(2025, 11, 1),
            new DateTime(2026, 3, 23),
            new DateTime(2026, 4, 26)
        ]);

        Assert.Equal(3, periods.Count);
        Assert.Equal(new DateTime(2025, 11, 1), periods[0].Effective);
        Assert.Equal(new DateTime(2026, 3, 22), periods[0].Expiry);
        Assert.Equal(new DateTime(2026, 3, 23), periods[1].Effective);
        Assert.Equal(new DateTime(2026, 4, 25), periods[1].Expiry);
        Assert.Equal(new DateTime(2026, 4, 26), periods[2].Effective);
        Assert.Null(periods[2].Expiry);
    }
}
