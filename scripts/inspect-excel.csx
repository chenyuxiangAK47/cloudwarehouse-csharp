#r "nuget: ClosedXML, 0.104.2"
using ClosedXML.Excel;
using System.Globalization;

var excelDir = Path.Combine(Environment.CurrentDirectory, "excel");
foreach (var path in Directory.GetFiles(excelDir, "*.xlsx"))
{
    Console.WriteLine(new string('=', 80));
    Console.WriteLine(Path.GetFileName(path));
    using var wb = new XLWorkbook(path);

    InspectSheet(wb, "_客户价格_ - 报价表", rows: 8);
    InspectSheet(wb, "_成本表_ - 成本表", rows: 8);
    InspectSheet(wb, "账单模版 - 账单明细", rows: 6);
    InspectSheet(wb, "2026-01 - 账单明细", rows: 6);
    InspectSampleBillRows(wb, "2026-01 - 账单明细", 5);
}

static void InspectSheet(XLWorkbook wb, string name, int rows)
{
    if (!wb.Worksheets.TryGetWorksheet(name, out var ws))
    {
        Console.WriteLine($"[missing] {name}");
        return;
    }

    Console.WriteLine($"\n--- {name} (used {ws.RangeUsed()?.RowCount() ?? 0} rows) ---");
    var used = ws.RangeUsed();
    if (used == null) return;

    int maxCol = Math.Min(used.LastColumn().ColumnNumber(), 35);
    for (int r = 1; r <= rows; r++)
    {
        var cells = new List<string>();
        for (int c = 1; c <= maxCol; c++)
        {
            var cell = ws.Cell(r, c);
            var text = cell.GetFormattedString().Trim();
            if (string.IsNullOrEmpty(text) && cell.HasFormula)
                text = $"={cell.FormulaA1}";
            if (!string.IsNullOrEmpty(text))
                cells.Add($"{Col(c)}:{text}");
        }
        if (cells.Count > 0)
            Console.WriteLine($"R{r}: {string.Join(" | ", cells)}");
    }
}

static void InspectSampleBillRows(XLWorkbook wb, string name, int dataRows)
{
    if (!wb.Worksheets.TryGetWorksheet(name, out var ws)) return;
    Console.WriteLine($"\n--- {name} sample data (first {dataRows} rows after header) ---");

    // find first row with waybill-like data (long number in col)
    int start = 3;
    for (int r = 3; r <= Math.Min(20, ws.LastRowUsed()?.RowNumber() ?? 3); r++)
    {
        var v = ws.Cell(r, 1).GetString();
        if (!string.IsNullOrWhiteSpace(v) && v.Any(char.IsDigit)) { start = r; break; }
    }

    var headers = new Dictionary<int, string>();
    for (int c = 1; c <= 40; c++)
    {
        var h1 = ws.Cell(1, c).GetFormattedString().Trim();
        var h2 = ws.Cell(2, c).GetFormattedString().Trim();
        var h = string.IsNullOrEmpty(h2) ? h1 : (string.IsNullOrEmpty(h1) ? h2 : $"{h1}/{h2}");
        if (!string.IsNullOrEmpty(h)) headers[c] = h;
    }

  for (int r = start; r < start + dataRows; r++)
    {
        var parts = new List<string>();
        foreach (var (c, h) in headers.OrderBy(x => x.Key).Take(25))
        {
            var cell = ws.Cell(r, c);
            var val = cell.GetFormattedString().Trim();
            if (string.IsNullOrEmpty(val) && cell.HasFormula)
                val = $"FORMULA({cell.FormulaA1})";
            if (!string.IsNullOrEmpty(val))
                parts.Add($"{h}={val}");
        }
        if (parts.Count > 0)
            Console.WriteLine($"Row{r}: {string.Join("; ", parts)}");
    }
}

static string Col(int n)
{
    string s = "";
    while (n > 0) { s = (char)('A' + (n - 1) % 26) + s; n = (n - 1) / 26; }
    return s;
}
