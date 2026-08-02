using CloudWarehouse.Backend.Helpers;

namespace CloudWarehouse.Tests;

public class WeightRoundingTests
{
    [Theory]
    [InlineData(0.1, "0.3")]
    [InlineData(0.3, "0.3")]
    [InlineData(0.4, "0.5")]
    [InlineData(0.26, "0.3")]
    [InlineData(2.19, "3")]
    [InlineData(5, "5")]
    public void RoundForForwardBilling_Within5Kg(decimal weight, string expected)
    {
        Assert.Equal(expected, WeightRounding.RoundForForwardBilling(weight));
    }

    [Fact]
    public void RoundForForwardBilling_Over5Kg_ReturnsMarker()
    {
        Assert.Equal(WeightRounding.Over5Marker, WeightRounding.RoundForForwardBilling(5.1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RoundForForwardBilling_NonPositive_ReturnsNull(decimal weight)
    {
        Assert.Null(WeightRounding.RoundForForwardBilling(weight));
    }

    [Fact]
    public void RoundWeight_2_19_Returns3()
    {
        Assert.Equal(3m, WeightRounding.RoundWeight(2.19m));
    }
}
