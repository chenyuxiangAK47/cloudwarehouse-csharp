using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Tests;

public class PriceCalculatorTests
{
    private static PriceTableRow CreateAnhuiSampleRow() => new()
    {
        Price_0_0_3 = 1.6m,
        Price_0_3_0_5 = 1.7m,
        Price_0_5_1 = 2.1m,
        Price_1_2 = 3.3m,
        Price_2_3 = 3.9m,
        Price_3_4 = 5m,
        Price_4_5 = 6m,
        BaseFee = 3.5m,
        AdditionalUnitPrice = 0.7m
    };

    [Theory]
    [InlineData(0.2, 1.6)]
    [InlineData(0.3, 1.6)]
    [InlineData(0.4, 1.7)]
    [InlineData(1.0, 2.1)]
    [InlineData(2.0, 3.3)]
    [InlineData(5.0, 6.0)]
    public void Calculate_Within5Kg_UsesCorrectTier(decimal weight, decimal expectedTierPrice)
    {
        var row = CreateAnhuiSampleRow();
        var result = PriceCalculator.Calculate(row, weight);
        Assert.Equal(expectedTierPrice + row.BaseFee, result);
    }

    [Fact]
    public void Calculate_1Kg_ReturnsTierPricePlusBaseFee()
    {
        var row = CreateAnhuiSampleRow();
        Assert.Equal(5.6m, PriceCalculator.Calculate(row, 1m));
    }

    [Fact]
    public void Calculate_5Kg_ReturnsTopTierPlusBaseFee()
    {
        var row = CreateAnhuiSampleRow();
        Assert.Equal(9.5m, PriceCalculator.Calculate(row, 5m));
    }

    [Fact]
    public void Calculate_10Kg_ReturnsBaseFeePlusAdditionalWeight()
    {
        var row = CreateAnhuiSampleRow();
        // 3.5 + (10-5)*0.7 = 7.0
        Assert.Equal(7.0m, PriceCalculator.Calculate(row, 10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Calculate_NonPositiveWeight_ReturnsNull(decimal weight)
    {
        var row = CreateAnhuiSampleRow();
        Assert.Null(PriceCalculator.Calculate(row, weight));
    }

    [Fact]
    public void Calculate_Over5Kg_WithoutAdditionalRate_ReturnsNull()
    {
        var row = CreateAnhuiSampleRow();
        row.AdditionalUnitPrice = 0;
        Assert.Null(PriceCalculator.Calculate(row, 10m));
    }

    [Fact]
    public void Calculate_MissingTierPrice_ReturnsNull()
    {
        var row = new PriceTableRow { BaseFee = 3.5m };
        Assert.Null(PriceCalculator.Calculate(row, 1m));
    }
}
