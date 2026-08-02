using CloudWarehouse.Backend.Helpers;

namespace CloudWarehouse.Tests;

public class CustomerQuote93FileTests
{
    [Fact]
    public void ReadCustomerQuotes_93Workbook_ParsesWithoutThrow()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "excel",
                "93-客户报价-单独.xlsx")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "excel",
                "[93] 小二小店-账单统计-2026年.xlsx"))
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path == null)
            return;

        using var stream = File.OpenRead(path);
        var ex = Record.Exception(() =>
        {
            var result = CustomerQuoteExcelHelper.ReadCustomerQuotes(stream);
            Assert.True(result.LongRows.Count > 50 || result.WideRows.Count > 0);
        });
        Assert.Null(ex);
    }
}
