using ClosedXML.Excel;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers;

public static class SiteExcelHelper
{
    private static readonly (string Key, string[] Headers)[] Columns =
    [
        ("code", ["站点编号", "SiteCode"]),
        ("name", ["站点名称", "SiteName"]),
        ("type", ["站点类型", "SiteType"]),
        ("express", ["快递公司", "ExpressCompany"]),
        ("contact", ["联系人", "ContactPerson"]),
        ("phone", ["联系电话", "ContactPhone"]),
        ("address", ["地址", "Address"]),
        ("status", ["状态", "Status"]),
        ("remark", ["备注", "Remark"])
    ];

    public static byte[] CreateImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("站点");
        ws.Cell(1, 1).Value = "站点编号";
        ws.Cell(1, 2).Value = "站点名称";
        ws.Cell(1, 3).Value = "站点类型";
        ws.Cell(1, 4).Value = "快递公司";
        ws.Cell(1, 5).Value = "联系人";
        ws.Cell(1, 6).Value = "联系电话";
        ws.Cell(1, 7).Value = "地址";
        ws.Cell(1, 8).Value = "状态";
        ws.Cell(1, 9).Value = "备注";
        ws.Cell(2, 1).Value = "C001";
        ws.Cell(2, 2).Value = "石家庄配送站";
        ws.Cell(2, 3).Value = 1;
        ws.Cell(2, 4).Value = "示例快递";
        ws.Cell(2, 7).Value = "河北省石家庄市";
        ws.Cell(2, 8).Value = 1;
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static List<SiteImportRow> ReadSites(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var (headerRow, colMap) = FindHeader(ws);
        if (!colMap.ContainsKey("code") || !colMap.ContainsKey("name"))
            throw new InvalidOperationException(
                "无法识别表头，请使用第 1 行：站点编号、站点名称（或 SiteCode / SiteName）。");

        var rows = new List<SiteImportRow>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var code = Get(ws, r, colMap, "code");
            var name = Get(ws, r, colMap, "name");
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name))
                continue;

            rows.Add(new SiteImportRow
            {
                RowNumber = r,
                SiteCode = code.Trim(),
                SiteName = name.Trim(),
                SiteType = ParseSiteType(Get(ws, r, colMap, "type")),
                ExpressCompany = Get(ws, r, colMap, "express"),
                ContactPerson = Get(ws, r, colMap, "contact"),
                ContactPhone = Get(ws, r, colMap, "phone"),
                Address = Get(ws, r, colMap, "address"),
                Status = ParseStatus(Get(ws, r, colMap, "status")),
                Remark = NullIfEmpty(Get(ws, r, colMap, "remark"))
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
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 15;
            for (var c = 1; c <= lastCol; c++)
            {
                var text = GetCell(ws, r, c);
                foreach (var (key, headers) in Columns)
                {
                    if (headers.Any(h => text.Equals(h, StringComparison.OrdinalIgnoreCase)))
                        map[key] = c;
                }
            }

            if (map.ContainsKey("code") && map.ContainsKey("name"))
                return (r, map);
        }

        return (1, new Dictionary<string, int>());
    }

    private static string Get(IXLWorksheet ws, int row, Dictionary<string, int> map, string key) =>
        map.TryGetValue(key, out var col) ? GetCell(ws, row, col) : string.Empty;

    private static string GetCell(IXLWorksheet ws, int row, int col) =>
        ws.Cell(row, col).GetFormattedString().Trim();

    private static int ParseSiteType(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        if (int.TryParse(text, out var n) && n is >= 1 and <= 3) return n;
        if (text.Contains('配')) return 1;
        if (text.Contains('中')) return 2;
        if (text.Contains('仓')) return 3;
        return 1;
    }

    private static int ParseStatus(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        if (int.TryParse(text, out var n) && (n == 0 || n == 1)) return n;
        if (text.Contains('禁')) return 0;
        return 1;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
