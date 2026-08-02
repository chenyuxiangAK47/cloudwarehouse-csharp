using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Services;
using CloudWarehouse.TestCommon;

namespace CloudWarehouse.Tests;

public class WaybillExcelHelperTests
{
    [Fact]
    public void ReadWaybills_StandardFormat_ParsesRows()
    {
        using var stream = WaybillExcelFactory.CreateStandardWorkbook();
        var result = WaybillExcelHelper.ReadWaybills(stream);

        Assert.Equal("标准格式", result.Format);
        Assert.Equal(1, result.HeaderRow);
        Assert.Single(result.Rows);

        var row = result.Rows[0];
        Assert.Equal("YT001", row.WaybillNo);
        Assert.Equal("安徽省", row.Province);
        Assert.Equal(2.19m, row.ActualWeight);
        Assert.Equal("测试账户", row.AccountName);
        Assert.Equal("圆通", row.ExpressType);
    }

    [Fact]
    public void ReadWaybills_SupplierDetailFormat_ParsesExtendedColumns()
    {
        using var stream = WaybillExcelFactory.CreateSupplierDetailWorkbook();
        var result = WaybillExcelHelper.ReadWaybills(stream);

        Assert.Equal("账单明细格式", result.Format);
        Assert.Single(result.Rows);

        var row = result.Rows[0];
        Assert.Equal("SF20260120001", row.WaybillNo);
        Assert.Equal("小二小店", row.AccountName);
        Assert.Equal("山西省", row.Province);
        Assert.Equal(1.8m, row.ActualWeight);
        Assert.Equal(5.5m, row.SourceTransitFee);
        Assert.Equal(3.5m, row.SourceLabelFee);
        Assert.Equal(1m, row.Surcharge);
    }

    [Fact]
    public void CreateStandardTemplate_RoundTrips()
    {
        var bytes = WaybillExcelHelper.CreateStandardTemplate();
        using var stream = new MemoryStream(bytes);
        var result = WaybillExcelHelper.ReadWaybills(stream);

        Assert.Equal("标准格式", result.Format);
        Assert.Single(result.Rows);
    }

    [Fact]
    public void ReadWaybills_DualHeaderBillDetail_ParsesComparisonColumns()
    {
        using var stream = DualHeaderBillDetailFactory.CreateSampleWorkbook();
        var result = WaybillExcelHelper.ReadWaybills(stream);

        Assert.Equal("账单明细双行表头", result.Format);
        Assert.Equal(2, result.HeaderRow);
        Assert.Single(result.Rows);

        var row = result.Rows[0];
        Assert.Equal("93", row.CustomerCode);
        Assert.Equal("YT20260101001", row.WaybillNo);
        Assert.Equal("云南省", row.Province);
        Assert.Equal(0.1m, row.ActualWeight);
        Assert.Equal(2.7m, row.ExpectedReceivableTransitFee);
        Assert.Equal(1.5m, row.ExpectedPayableTransitFee);
        Assert.Equal(4m, row.ReceivablePrepayment);
        Assert.Equal(-2.5m, row.PayablePrepayment);
    }

    [Fact]
    public void ExportResult_ReturnsNonEmptyXlsx()
    {
        using var stream = WaybillExcelFactory.CreateStandardWorkbook();
        var parsed = WaybillExcelHelper.ReadWaybills(stream);
        parsed.Rows[0].RoundedWeight = 3m;
        parsed.Rows[0].ReceivableTotal = 8.5m;
        parsed.Rows[0].PayableTotal = 8.5m;
        parsed.Rows[0].Profit = 0m;

        var bytes = WaybillExcelHelper.ExportResult(parsed.Rows);
        Assert.True(bytes.Length > 200);
    }
}

public class BillImportServiceRegionTests
{
    [Theory]
    [InlineData("安徽省", "安徽")]
    [InlineData("山西省", "山西")]
    [InlineData("北京市", "北京")]
    [InlineData("内蒙古自治区", "内蒙古")]
    public void NormalizeRegion_StripsAdministrativeSuffix(string input, string expected)
    {
        Assert.Equal(expected, BillImportService.NormalizeRegion(input));
    }
}
