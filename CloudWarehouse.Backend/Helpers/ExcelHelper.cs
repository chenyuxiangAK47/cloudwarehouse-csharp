using ClosedXML.Excel;
using CloudWarehouse.Backend.Models;
using System.Globalization;

namespace CloudWarehouse.Backend.Helpers;

public static class ExcelHelper
{
    private const int LegacyHeaderRow = 3;
    private const int LegacyDataStartRow = 4;
    private const int StandardHeaderRow = 1;
    private const int StandardDataStartRow = 2;

    private static readonly string[] StandardHeaders =
    [
        "生效时间", "站点编号", "目的地代码", "目的地",
        "0kg<X<=0.3kg", "0.3kg<X<=0.5kg", "0.5kg<X<=1kg",
        "1kg<X<=2kg", "2kg<X<=3kg", "3kg<X<=4kg", "4kg<X<=5kg",
        "面单费", "续重(元/kg)"
    ];

    /// <summary>自动识别：第1行表头=标准格式；第3行表头=供应商三级表头。</summary>
    public static PriceTableImportResult ReadPriceTable(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Contains("价格表", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var headerRow = DetectHeaderRow(worksheet);
        var dataStartRow = headerRow + 1;

        return ParseWorksheet(worksheet, headerRow, dataStartRow,
            headerRow == StandardHeaderRow ? "标准格式" : "供应商三级表头");
    }

    /// <summary>生成云仓标准导入模板（第1行表头，第2行示例）。</summary>
    public static byte[] CreateStandardPriceTableTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("价格表");

        for (int c = 0; c < StandardHeaders.Length; c++)
            ws.Cell(1, c + 1).Value = StandardHeaders[c];

        ws.Cell(2, 1).Value = new DateTime(2026, 5, 7);
        ws.Cell(2, 2).Value = "C001";
        ws.Cell(2, 3).Value = "11";
        ws.Cell(2, 4).Value = "安徽省";
        ws.Cell(2, 5).Value = 1.6;
        ws.Cell(2, 6).Value = 1.7;
        ws.Cell(2, 7).Value = 2.1;
        ws.Cell(2, 8).Value = 3.3;
        ws.Cell(2, 9).Value = 3.9;
        ws.Cell(2, 10).Value = 5;
        ws.Cell(2, 11).Value = 6;
        ws.Cell(2, 12).Value = 3.5;
        ws.Cell(2, 13).Value = 0.7;

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public static byte[] ExportPriceTableResult(IEnumerable<PriceTableRow> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("导入结果");

        var headers = new[]
        {
            "行号", "状态", "生效时间", "站点编号", "目的地代码", "目的地",
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
            ws.Cell(r, 2).Value = string.IsNullOrEmpty(row.ErrorMessage) ? "成功" : row.ErrorMessage;
            ws.Cell(r, 3).Value = row.EffectiveDate?.ToString("yyyy/M/d") ?? "";
            ws.Cell(r, 4).Value = row.SiteCode;
            ws.Cell(r, 5).Value = row.DestCode;
            ws.Cell(r, 6).Value = row.Destination;
            WriteDecimal(ws.Cell(r, 7), row.Price_0_0_3);
            WriteDecimal(ws.Cell(r, 8), row.Price_0_3_0_5);
            WriteDecimal(ws.Cell(r, 9), row.Price_0_5_1);
            WriteDecimal(ws.Cell(r, 10), row.Price_1_2);
            WriteDecimal(ws.Cell(r, 11), row.Price_2_3);
            WriteDecimal(ws.Cell(r, 12), row.Price_3_4);
            WriteDecimal(ws.Cell(r, 13), row.Price_4_5);
            ws.Cell(r, 14).Value = row.BaseFee;
            ws.Cell(r, 15).Value = row.AdditionalUnitPrice;
            WriteDecimal(ws.Cell(r, 16), row.ExpectedPrice1Kg);
            WriteDecimal(ws.Cell(r, 17), row.ExpectedPrice5Kg);
            WriteDecimal(ws.Cell(r, 18), row.ExpectedPrice10Kg);
            r++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static int DetectHeaderRow(IXLWorksheet worksheet)
    {
        var row1 = GetCellText(worksheet, 1, 1);
        if (row1.Contains("生效时间", StringComparison.OrdinalIgnoreCase))
            return StandardHeaderRow;

        var row3 = GetCellText(worksheet, 3, 1);
        if (row3.Contains("生效时间", StringComparison.OrdinalIgnoreCase))
            return LegacyHeaderRow;

        throw new InvalidOperationException(
            "无法识别 Excel 格式。请使用「下载标准模板」填写（第1行表头），或供应商三级表头（第3行表头）。");
    }

    private static PriceTableImportResult ParseWorksheet(
        IXLWorksheet worksheet, int headerRow, int dataStartRow, string format)
    {
        var result = new PriceTableImportResult
        {
            SheetName = worksheet.Name,
            Format = format,
            HeaderRow = headerRow,
            DataStartRow = dataStartRow
        };

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? dataStartRow;
        for (int rowNum = dataStartRow; rowNum <= lastRow; rowNum++)
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
            result.Warnings.Add("未解析到有效数据行。");

        return result;
    }

    private static string GetCellText(IXLWorksheet ws, int row, int col) =>
        ws.Cell(row, col).GetFormattedString().Trim();

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
        if (value.HasValue) cell.Value = value.Value;
    }
}
