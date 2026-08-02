using ClosedXML.Excel;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers;

public static class DestinationExcelHelper
{
    private static readonly (string Key, string[] Headers)[] Columns =
    [
        ("code", ["目的地编码", "目的地代码", "DestCode"]),
        ("province", ["省份", "Province"]),
        ("city", ["城市", "City"]),
        ("area", ["区域", "Area"])
    ];

    public static byte[] CreateImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("目的地");
        ws.Cell(1, 1).Value = "目的地编码";
        ws.Cell(1, 2).Value = "省份";
        ws.Cell(1, 3).Value = "城市";
        ws.Cell(1, 4).Value = "区域";
        ws.Cell(2, 1).Value = "11";
        ws.Cell(2, 2).Value = "安徽省";
        ws.Cell(2, 3).Value = "";
        ws.Cell(2, 4).Value = "";
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static List<DestinationImportRow> ReadDestinations(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var (headerRow, colMap) = FindHeader(ws);
        if (!colMap.ContainsKey("code") || !colMap.ContainsKey("province"))
            throw new InvalidOperationException(
                "无法识别表头，请使用第 1 行：目的地编码、省份（或 DestCode / Province）。");

        var rows = new List<DestinationImportRow>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var code = Get(ws, r, colMap, "code");
            var province = Get(ws, r, colMap, "province");
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(province))
                continue;

            rows.Add(new DestinationImportRow
            {
                RowNumber = r,
                DestCode = code.Trim(),
                Province = province.Trim(),
                City = Get(ws, r, colMap, "city"),
                Area = Get(ws, r, colMap, "area")
            });
        }

        if (rows.Count == 0)
            throw new InvalidOperationException("表头下方未找到有效数据行。");

        return rows;
    }

    private static (int headerRow, Dictionary<string, int> colMap) FindHeader(IXLWorksheet ws)
    {
        var lastRow = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 1, 5);
        for (var r = 1; r <= lastRow; r++)
        {
            var map = new Dictionary<string, int>();
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 10;
            for (var c = 1; c <= lastCol; c++)
            {
                var text = GetCell(ws, r, c);
                foreach (var (key, headers) in Columns)
                {
                    if (headers.Any(h => text.Equals(h, StringComparison.OrdinalIgnoreCase)))
                        map[key] = c;
                }
            }

            if (map.ContainsKey("code") && map.ContainsKey("province"))
                return (r, map);
        }

        return (1, new Dictionary<string, int>());
    }

    private static string Get(IXLWorksheet ws, int row, Dictionary<string, int> map, string key) =>
        map.TryGetValue(key, out var col) ? GetCell(ws, row, col) : string.Empty;

    private static string GetCell(IXLWorksheet ws, int row, int col) =>
        ws.Cell(row, col).GetFormattedString().Trim();
}
