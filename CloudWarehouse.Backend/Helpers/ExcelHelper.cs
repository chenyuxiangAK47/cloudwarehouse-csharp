using ClosedXML.Excel;
using CloudWarehouse.Backend.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CloudWarehouse.Backend.Helpers;

public static class ExcelHelper
{
    private const int PriceTableHeaderRow = 3;
    private const int PriceTableDataStartRow = 4;
    private const int PriceTableColumnCount = 13;

    /// <summary>
    /// 解析三级表头价格表：第 3 行为列名，第 4 行起为数据（A-M 共 13 列）。
    /// </summary>
    public static PriceTableImportResult ReadPriceTable(Stream stream)
    {
        var result = new PriceTableImportResult
        {
            HeaderRow = PriceTableHeaderRow,
            DataStartRow = PriceTableDataStartRow
        };

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Contains("价格表", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        result.SheetName = worksheet.Name;

        var headerA3 = GetCellText(worksheet, PriceTableHeaderRow, 1);
        if (!headerA3.Contains("生效时间", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"未识别为价格表格式：第 {PriceTableHeaderRow} 行 A 列应为「生效时间」，实际为「{headerA3}」。" +
                "请确认使用第 3 行作为列名、第 4 行起为数据的三级表头模板。");
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? PriceTableDataStartRow;
        for (int rowNum = PriceTableDataStartRow; rowNum <= lastRow; rowNum++)
        {
            var destCode = GetCellText(worksheet, rowNum, 3);
            var destination = GetCellText(worksheet, rowNum, 4);
            if (string.IsNullOrWhiteSpace(destCode) && string.IsNullOrWhiteSpace(destination))
                continue;

            var row = new PriceTableRow
            {
                RowNumber = rowNum,
                EffectiveDate = ParseDate(GetCellText(worksheet, rowNum, 1)),
                SiteCode = GetCellText(worksheet, rowNum, 2),
                DestCode = destCode,
                Destination = destination,
                Price_0_0_3 = ParseDecimal(worksheet.Cell(rowNum, 5)),
                Price_0_3_0_5 = ParseDecimal(worksheet.Cell(rowNum, 6)),
                Price_0_5_1 = ParseDecimal(worksheet.Cell(rowNum, 7)),
                Price_1_2 = ParseDecimal(worksheet.Cell(rowNum, 8)),
                Price_2_3 = ParseDecimal(worksheet.Cell(rowNum, 9)),
                Price_3_4 = ParseDecimal(worksheet.Cell(rowNum, 10)),
                Price_4_5 = ParseDecimal(worksheet.Cell(rowNum, 11)),
                BaseFee = ParseDecimal(worksheet.Cell(rowNum, 12)) ?? 3.5m,
                AdditionalUnitPrice = ParseDecimal(worksheet.Cell(rowNum, 13)) ?? 0m
            };

            row.ExpectedPrice1Kg = PriceCalculator.Calculate(row, 1m);
            row.ExpectedPrice5Kg = PriceCalculator.Calculate(row, 5m);
            row.ExpectedPrice10Kg = PriceCalculator.Calculate(row, 10m);

            result.Rows.Add(row);
        }

        result.TotalRows = result.Rows.Count;
        if (result.TotalRows == 0)
            result.Warnings.Add("未解析到有效数据行，请检查第 4 行起是否有目的地代码或名称。");

        return result;
    }

    public static byte[] ExportPriceTableResult(IEnumerable<PriceTableRow> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("导入结果");

        var headers = new[]
        {
            "行号", "生效时间", "站点编号", "目的地代码", "目的地",
            "0-0.3kg", "0.3-0.5kg", "0.5-1kg", "1-2kg", "2-3kg", "3-4kg", "4-5kg",
            "面单费", "续重(元/kg)",
            "预期价格(1kg)", "预期价格(5kg)", "预期价格(10kg)"
        };

        for (int c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        int r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.RowNumber;
            ws.Cell(r, 2).Value = row.EffectiveDate?.ToString("yyyy/M/d") ?? "";
            ws.Cell(r, 3).Value = row.SiteCode;
            ws.Cell(r, 4).Value = row.DestCode;
            ws.Cell(r, 5).Value = row.Destination;
            WriteDecimal(ws.Cell(r, 6), row.Price_0_0_3);
            WriteDecimal(ws.Cell(r, 7), row.Price_0_3_0_5);
            WriteDecimal(ws.Cell(r, 8), row.Price_0_5_1);
            WriteDecimal(ws.Cell(r, 9), row.Price_1_2);
            WriteDecimal(ws.Cell(r, 10), row.Price_2_3);
            WriteDecimal(ws.Cell(r, 11), row.Price_3_4);
            WriteDecimal(ws.Cell(r, 12), row.Price_4_5);
            ws.Cell(r, 13).Value = row.BaseFee;
            ws.Cell(r, 14).Value = row.AdditionalUnitPrice;
            WriteDecimal(ws.Cell(r, 15), row.ExpectedPrice1Kg);
            WriteDecimal(ws.Cell(r, 16), row.ExpectedPrice5Kg);
            WriteDecimal(ws.Cell(r, 17), row.ExpectedPrice10Kg);
            r++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string GetCellText(IXLWorksheet ws, int row, int col)
    {
        return ws.Cell(row, col).GetFormattedString().Trim();
    }

    private static decimal? ParseDecimal(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        if (cell.DataType == XLDataType.Number)
            return (decimal)cell.GetDouble();

        var text = cell.GetFormattedString().Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;

        text = text.Replace("元", "").Replace("/kg", "").Replace(",", "");
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
            return v;

        return null;
    }

    private static DateTime? ParseDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
            return dt;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;
        return null;
    }

    private static void WriteDecimal(IXLCell cell, decimal? value)
    {
        if (value.HasValue)
            cell.Value = value.Value;
    }
}
