using System.Diagnostics;
using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.TestCommon;

namespace CloudWarehouse.Tests;

/// <summary>
/// Baseline numbers for report Section 18.6 / appendix.
/// Parse-only (no SQL) so CI can run without a database.
/// </summary>
public class Import1000RowPerfTests
{
    [Fact]
    public void ParseStandardPriceTable_1000Rows_Under30Seconds()
    {
        using var stream = PriceTableExcelFactory.CreateStandardFormatWorkbookWithRowCount(1000);
        var sw = Stopwatch.StartNew();
        var result = ExcelHelper.ReadPriceTable(stream);
        sw.Stop();

        Assert.True(result.Rows.Count >= 1000,
            $"Expected >=1000 parsed rows, got {result.Rows.Count}");
        Assert.True(sw.Elapsed.TotalSeconds < 30,
            $"1000-row parse took {sw.Elapsed.TotalSeconds:F2}s (target <30s)");

        // Visible in `dotnet test -v n` for appendix paste
        Console.WriteLine(
            $"[PERF] ExcelHelper.ReadPriceTable 1000 rows: {sw.ElapsedMilliseconds} ms, rows={result.Rows.Count}");
    }
}
