using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using CloudWarehouse.Backend.Services;
using CloudWarehouse.TestCommon;

namespace CloudWarehouse.Tests;

public class WeightBracketParserTests
{
    [Theory]
    [InlineData("0.3", 0, 0.3, 1)]
    [InlineData("3", 2, 3, 1)]
    [InlineData(">5", 5, 99999, 2)]
    public void Parse_MapsBracketToRange(string label, decimal min, decimal max, int billingType)
    {
        var result = WeightBracketParser.Parse(label);
        Assert.NotNull(result);
        Assert.Equal(min, result.Value.Min);
        Assert.Equal(max, result.Value.Max);
        Assert.Equal(billingType, result.Value.BillingType);
    }
}

public class CustomerQuoteExcelHelperTests
{
    [Fact]
    public void ReadCustomerQuotes_StandardWideFormat_ParsesRow()
    {
        using var stream = CustomerQuoteExcelFactory.CreateStandardWideWorkbook();
        var result = CustomerQuoteExcelHelper.ReadCustomerQuotes(stream);

        Assert.Equal("标准格式", result.Format);
        Assert.Single(result.WideRows);
        Assert.Equal("A0001", result.WideRows[0].CustomerCode);
        Assert.Equal("jiangxi", result.WideRows[0].Province);
        Assert.Equal(9.9m, result.WideRows[0].Price_2_3);
    }

    [Fact]
    public void CreateStandardTemplate_RoundTrips()
    {
        var bytes = CustomerQuoteExcelHelper.CreateStandardTemplate();
        using var stream = new MemoryStream(bytes);
        var result = CustomerQuoteExcelHelper.ReadCustomerQuotes(stream);
        Assert.Equal("标准格式", result.Format);
        Assert.Single(result.WideRows);
    }

    [Fact]
    public void ReadCustomerQuotes_WaybillBillDetailSheet_ThrowsClearMessage()
    {
        using var stream = DualHeaderBillDetailFactory.CreateSampleWorkbook();
        var ex = Assert.Throws<InvalidOperationException>(() => CustomerQuoteExcelHelper.ReadCustomerQuotes(stream));
        Assert.Contains("运单", ex.Message);
        Assert.Contains("运单导入", ex.Message);
    }
}

public class CustomerQuoteCalculateServiceTests
{
    [Fact]
    public void CalculateFromRules_MatchesProvinceAndWeightTier()
    {
        var rules = new List<CustomerQuoteRule>
        {
            new()
            {
                CustomerId = 1, Province = "安徽省", ExpressType = "圆通",
                BillingType = 1, MinWeight = 2m, MaxWeight = 3m,
                UnitPrice = 8m, BaseFee = 2m, EffectiveDate = new DateTime(2026, 1, 1), Status = 1
            },
            new()
            {
                CustomerId = 1, Province = "安徽省", ExpressType = "圆通",
                BillingType = 2, MinWeight = 5m, MaxWeight = 99999m,
                UnitPrice = 1.5m, BaseFee = 2m, EffectiveDate = new DateTime(2026, 1, 1), Status = 1
            }
        };

        var result = CustomerQuoteCalculateService.CalculateFromRules(
            rules, "安徽省", "圆通", 3m, new DateTime(2026, 6, 1));

        Assert.NotNull(result);
        Assert.Equal(8m, result.WeightFee);
        Assert.Equal(2m, result.BaseFee);
        Assert.Equal(10m, result.TotalPrice);
    }

    [Fact]
    public void CalculateFromRules_CustomerPriceHigherThanCost_ProducesProfitScenario()
    {
        var customerRules = new List<CustomerQuoteRule>
        {
            new()
            {
                CustomerId = 1, Province = "jiangxi",
                BillingType = 1, MinWeight = 2m, MaxWeight = 3m,
                UnitPrice = 10m, BaseFee = 4m, EffectiveDate = new DateTime(2026, 5, 7), Status = 1
            }
        };

        var costRules = new List<PriceRule>
        {
            new()
            {
                BillingType = 1, MinWeight = 2m, MaxWeight = 3m,
                UnitPrice = 3.9m, BaseFee = 3.5m, EffectiveDate = new DateTime(2026, 5, 7), Status = 1
            }
        };

        var recv = CustomerQuoteCalculateService.CalculateFromRules(
            customerRules, "jiangxi", null, 3m, new DateTime(2026, 6, 1));
        var pay = FeeRuleCalculator.Calculate(costRules, 3m, new DateTime(2026, 6, 1));

        Assert.NotNull(recv);
        Assert.NotNull(pay);
        Assert.True(recv.TotalPrice > pay.TotalPrice);
    }
}
