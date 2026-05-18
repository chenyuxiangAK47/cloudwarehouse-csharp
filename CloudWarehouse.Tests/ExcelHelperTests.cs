using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.TestCommon;

namespace CloudWarehouse.Tests;

public class ExcelHelperTests
{
    [Fact]
    public void ReadPriceTable_StandardFormat_ParsesFromRow2()
    {
        using var stream = PriceTableExcelFactory.CreateStandardFormatWorkbook();
        var result = ExcelHelper.ReadPriceTable(stream);

        Assert.Equal("标准格式", result.Format);
        Assert.Equal(1, result.HeaderRow);
        Assert.Equal(2, result.DataStartRow);
        Assert.Equal(1, result.TotalRows);
    }

    [Fact]
    public void CreateStandardPriceTableTemplate_ReturnsValidXlsx()
    {
        var bytes = ExcelHelper.CreateStandardPriceTableTemplate();
        Assert.True(bytes.Length > 100);
        using var stream = new MemoryStream(bytes);
        var result = ExcelHelper.ReadPriceTable(stream);
        Assert.Equal("标准格式", result.Format);
    }

    [Fact]
    public void ReadPriceTable_ValidWorkbook_ParsesRowsAndExpectedPrices()
    {
        using var stream = PriceTableExcelFactory.CreateValidWorkbook(
            ws => PriceTableExcelFactory.FillSampleRow(ws, 4, "11", "安徽省"),
            ws => PriceTableExcelFactory.FillSampleRow(ws, 5, "12", "福建省"));

        var result = ExcelHelper.ReadPriceTable(stream);

        Assert.Equal("价格表", result.SheetName);
        Assert.Equal(3, result.HeaderRow);
        Assert.Equal(4, result.DataStartRow);
        Assert.Equal(2, result.TotalRows);
        Assert.Empty(result.Warnings);

        var anhui = result.Rows[0];
        Assert.Equal(4, anhui.RowNumber);
        Assert.Equal("C001", anhui.SiteCode);
        Assert.Equal("11", anhui.DestCode);
        Assert.Equal("安徽省", anhui.Destination);
        Assert.Equal(3.5m, anhui.BaseFee);
        Assert.Equal(0.7m, anhui.AdditionalUnitPrice);
        Assert.Equal(5.6m, anhui.ExpectedPrice1Kg);
        Assert.Equal(9.5m, anhui.ExpectedPrice5Kg);
        Assert.Equal(7.0m, anhui.ExpectedPrice10Kg);
    }

    [Fact]
    public void ReadPriceTable_InvalidHeader_Throws()
    {
        using var stream = PriceTableExcelFactory.CreateInvalidHeaderWorkbook();
        var ex = Assert.Throws<InvalidOperationException>(() => ExcelHelper.ReadPriceTable(stream));
        Assert.Contains("无法识别", ex.Message);
    }

    [Fact]
    public void ReadPriceTable_NoDataRows_AddsWarning()
    {
        using var stream = PriceTableExcelFactory.CreateValidWorkbook();
        // 仅表头，无数据行 configurators 默认有一行 - 需要只有表头

        using var emptyStream = PriceTableExcelFactory.CreateHeaderOnlyWorkbook();
        var result = ExcelHelper.ReadPriceTable(emptyStream);

        Assert.Equal(0, result.TotalRows);
        Assert.Contains(result.Warnings, w => w.Contains("未解析到有效数据行"));
    }

    [Fact]
    public void ExportPriceTableResult_ReturnsNonEmptyXlsx()
    {
        using var stream = PriceTableExcelFactory.CreateValidWorkbook();
        var import = ExcelHelper.ReadPriceTable(stream);

        var bytes = ExcelHelper.ExportPriceTableResult(import.Rows);

        Assert.NotEmpty(bytes);
        Assert.Equal(0x50, bytes[0]); // PK zip header
        Assert.Equal(0x4B, bytes[1]);
    }

    [Fact]
    public void ReadPriceTable_SkipsBlankDataRows()
    {
        using var stream = PriceTableExcelFactory.CreateValidWorkbook(
            ws => PriceTableExcelFactory.FillSampleRow(ws, 4, "11", "安徽省"),
            ws => { /* 空行：无目的地代码与名称 */ },
            ws => PriceTableExcelFactory.FillSampleRow(ws, 6, "12", "福建省"));

        var result = ExcelHelper.ReadPriceTable(stream);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal("11", result.Rows[0].DestCode);
        Assert.Equal("12", result.Rows[1].DestCode);
    }
}
