using CloudWarehouse.Backend.Helpers;

namespace CloudWarehouse.Tests;

public class Waybill93FileTests
{
    [Fact]
    public void ReadWaybills_2026BillDetailSheet_ParsesDualHeader()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "excel",
            "2026-01-账单明细-单独.xlsx"));

        if (!File.Exists(path))
            return;

        using var stream = File.OpenRead(path);
        var result = WaybillExcelHelper.ReadWaybills(stream);

        Assert.Equal("账单明细双行表头", result.Format);
        Assert.True(result.Rows.Count > 100);
        Assert.True(result.Rows.Any(r => r.ExpectedReceivableTransitFee.HasValue));
        Assert.True(result.Rows.Any(r => r.ExpectedPayableTransitFee.HasValue));
    }
}
