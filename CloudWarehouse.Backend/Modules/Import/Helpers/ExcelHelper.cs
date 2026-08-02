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

    /// <summary>自动识别：标准 / 三级表头 / 目的地矩阵（仅目的地列+价格列）。</summary>
    public static PriceTableImportResult ReadPriceTable(Stream stream, PriceTableImportOptions? options = null)
    {
        options ??= new PriceTableImportOptions();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Contains("价格表", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var matrixHeaderRow = DetectDestinationMatrixHeaderRow(worksheet);
        if (matrixHeaderRow > 0)
            return ParseDestinationMatrix(worksheet, matrixHeaderRow, options);

        if (DetectMasterCostLongFormat(worksheet))
            return ParseMasterCostLongFormat(worksheet);

        var headerRow = DetectLegacyOrStandardHeaderRow(worksheet);
        var dataStartRow = headerRow + 1;

        return ParseLaneWorksheet(worksheet, headerRow, dataStartRow,
            headerRow == StandardHeaderRow ? "标准格式" : "供应商三级表头", options);
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

    private static int DetectDestinationMatrixHeaderRow(IXLWorksheet worksheet)
    {
        for (var r = 1; r <= 5; r++)
        {
            var c1 = GetCellText(worksheet, r, 1);
            if (c1 == "目的地" || c1.StartsWith("目的地", StringComparison.OrdinalIgnoreCase))
                return r;
        }
        return 0;
    }

    private static int DetectLegacyOrStandardHeaderRow(IXLWorksheet worksheet)
    {
        var row1 = GetCellText(worksheet, 1, 1);
        if (row1.Contains("生效时间", StringComparison.OrdinalIgnoreCase))
            return StandardHeaderRow;

        var row3 = GetCellText(worksheet, 3, 1);
        if (row3.Contains("生效时间", StringComparison.OrdinalIgnoreCase))
            return LegacyHeaderRow;

        throw new InvalidOperationException(
            "无法识别 Excel 格式。支持：云仓标准模板、供应商三级表头、目的地矩阵价目表、师傅成本表（快递类型+地区+重量范围）。");
    }

    /// <summary>师傅成本表：每行一条「快递类型 + 地区 + 重量段 + 中转费」。</summary>
    private static bool DetectMasterCostLongFormat(IXLWorksheet worksheet)
    {
        var col1 = GetCellText(worksheet, 1, 1);
        var col3 = GetCellText(worksheet, 1, 3);
        return col1.Contains("快递类型", StringComparison.OrdinalIgnoreCase)
            && col3.Contains("地区", StringComparison.OrdinalIgnoreCase);
    }

    private static PriceTableImportResult ParseMasterCostLongFormat(IXLWorksheet worksheet)
    {
        var standardBrackets = new (decimal Min, decimal Max)[]
        {
            (0m, 0.3m), (0.3m, 0.5m), (0.5m, 1m), (1m, 2m), (2m, 3m), (3m, 4m), (4m, 5m)
        };

        var laneBrackets = new Dictionary<(string Express, string Province),
            List<(decimal Min, decimal Max, List<PriceHistoryVersion> Versions)>>();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 3;
        for (var r = 3; r <= lastRow; r++)
        {
            var express = GetCellText(worksheet, r, 1);
            var province = GetCellText(worksheet, r, 3);
            if (string.IsNullOrWhiteSpace(express) || string.IsNullOrWhiteSpace(province))
                continue;

            var minW = ParseDecimal(worksheet.Cell(r, 4));
            var maxW = ParseDecimal(worksheet.Cell(r, 5));
            if (minW == null || maxW == null || maxW > 5m)
                continue;

            var versions = MasterPriceHistoryHelper.ReadVersions(
                worksheet, r, MasterPriceHistoryHelper.CostHistoryColumns);
            if (versions.Count == 0)
            {
                var fallbackPrice = PickMasterCostFee(worksheet, r);
                if (fallbackPrice == null)
                    continue;

                var effCell = worksheet.Cell(r, 7);
                var effective = effCell.DataType == XLDataType.DateTime
                    ? effCell.GetDateTime().Date
                    : DateTime.Today;
                versions = [new PriceHistoryVersion(effective, fallbackPrice.Value)];
            }

            var key = (express.Trim(), province.Trim());
            if (!laneBrackets.TryGetValue(key, out var brackets))
            {
                brackets = [];
                laneBrackets[key] = brackets;
            }

            brackets.Add((minW.Value, maxW.Value, versions));
        }

        var result = new PriceTableImportResult
        {
            SheetName = worksheet.Name,
            Format = "师傅成本表(地区+重量段)",
            HeaderRow = 1,
            DataStartRow = 3
        };

        foreach (var lane in laneBrackets.OrderBy(x => x.Key.Express, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(x => x.Key.Province, StringComparer.OrdinalIgnoreCase))
        {
            var allVersionDates = lane.Value
                .SelectMany(b => b.Versions.Select(v => v.EffectiveDate));
            var periods = MasterPriceHistoryHelper.BuildEffectivePeriods(allVersionDates);

            foreach (var (effective, expiry) in periods)
            {
                var row = new PriceTableRow
                {
                    RowNumber = result.Rows.Count + 1,
                    EffectiveDate = effective,
                    ExpiryDate = expiry,
                    SiteCode = lane.Key.Express,
                    DestCode = lane.Key.Province,
                    Destination = lane.Key.Province,
                    BaseFee = 0m,
                    AdditionalUnitPrice = 5m
                };

                foreach (var (min, max, versions) in lane.Value)
                {
                    var price = MasterPriceHistoryHelper.PriceAtDate(versions, effective);
                    if (price != null)
                        AssignMasterCostTierPrice(row, min, max, price.Value);
                }

                ForwardFillMasterCostTiers(row, standardBrackets);
                if (!HasAnyTierPrice(row))
                    continue;

                row.ExpectedPrice1Kg = PriceCalculator.Calculate(row, 1m);
                row.ExpectedPrice5Kg = PriceCalculator.Calculate(row, 5m);
                row.ExpectedPrice10Kg = PriceCalculator.Calculate(row, 10m);
                result.Rows.Add(row);
            }
        }

        result.TotalRows = result.Rows.Count;
        if (result.TotalRows == 0)
            result.Warnings.Add("未解析到有效数据行。");

        return result;
    }

    private static void AssignMasterCostTierPrice(PriceTableRow row, decimal min, decimal max, decimal price)
    {
        if (min == 0m && max == 0.3m) row.Price_0_0_3 = price;
        else if (min == 0.3m && max == 0.5m) row.Price_0_3_0_5 = price;
        else if (min == 0.5m && max == 1m) row.Price_0_5_1 = price;
        else if (min == 1m && max == 2m) row.Price_1_2 = price;
        else if (min == 2m && max == 3m) row.Price_2_3 = price;
        else if (min == 3m && max == 4m) row.Price_3_4 = price;
        else if (min == 4m && max == 5m) row.Price_4_5 = price;
    }

    private static void ForwardFillMasterCostTiers(
        PriceTableRow row,
        IReadOnlyList<(decimal Min, decimal Max)> brackets)
    {
        decimal? last = null;
        for (var i = 0; i < brackets.Count; i++)
        {
            var current = GetMasterCostTierPrice(row, brackets[i].Min, brackets[i].Max);
            if (current != null)
                last = current;
            else if (last != null)
                AssignMasterCostTierPrice(row, brackets[i].Min, brackets[i].Max, last.Value);
        }
    }

    private static decimal? GetMasterCostTierPrice(PriceTableRow row, decimal min, decimal max)
    {
        if (min == 0m && max == 0.3m) return row.Price_0_0_3;
        if (min == 0.3m && max == 0.5m) return row.Price_0_3_0_5;
        if (min == 0.5m && max == 1m) return row.Price_0_5_1;
        if (min == 1m && max == 2m) return row.Price_1_2;
        if (min == 2m && max == 3m) return row.Price_2_3;
        if (min == 3m && max == 4m) return row.Price_3_4;
        if (min == 4m && max == 5m) return row.Price_4_5;
        return null;
    }

    private static bool HasAnyTierPrice(PriceTableRow row) =>
        row.Price_0_0_3 != null
        || row.Price_0_3_0_5 != null
        || row.Price_0_5_1 != null
        || row.Price_1_2 != null
        || row.Price_2_3 != null
        || row.Price_3_4 != null
        || row.Price_4_5 != null;

    private static decimal? PickMasterCostFee(IXLWorksheet worksheet, int row)
    {
        foreach (var col in new[] { 8, 11, 14 })
        {
            var fee = ParseDecimal(worksheet.Cell(row, col));
            if (fee != null)
                return fee;
        }

        return null;
    }

    /// <summary>矩阵价目表：第1列目的地，随后7档重量价，面单费，续重；站点来自导入参数。</summary>
    private static PriceTableImportResult ParseDestinationMatrix(
        IXLWorksheet worksheet, int headerRow, PriceTableImportOptions options)
    {
        var siteCode = options.ResolveSiteCode();

        var dataStartRow = headerRow + 1;
        if (GetCellText(worksheet, dataStartRow, 2).Contains("kg", StringComparison.OrdinalIgnoreCase))
            dataStartRow++;

        var result = new PriceTableImportResult
        {
            SheetName = worksheet.Name,
            Format = "目的地矩阵价目表",
            HeaderRow = headerRow,
            DataStartRow = dataStartRow
        };

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? dataStartRow;
        var effective = DateTime.Today;

        for (var rowNum = dataStartRow; rowNum <= lastRow; rowNum++)
        {
            var destination = GetCellText(worksheet, rowNum, 1);
            if (string.IsNullOrWhiteSpace(destination))
                continue;
            if (destination.Contains("目的地", StringComparison.OrdinalIgnoreCase))
                continue;

            var row = new PriceTableRow
            {
                RowNumber = rowNum,
                EffectiveDate = effective,
                SiteCode = siteCode,
                DestCode = destination.Trim(),
                Destination = destination.Trim(),
                Price_0_0_3 = ParseDecimal(worksheet.Cell(rowNum, 2)),
                Price_0_3_0_5 = ParseDecimal(worksheet.Cell(rowNum, 3)),
                Price_0_5_1 = ParseDecimal(worksheet.Cell(rowNum, 4)),
                Price_1_2 = ParseDecimal(worksheet.Cell(rowNum, 5)),
                Price_2_3 = ParseDecimal(worksheet.Cell(rowNum, 6)),
                Price_3_4 = ParseDecimal(worksheet.Cell(rowNum, 7)),
                Price_4_5 = ParseDecimal(worksheet.Cell(rowNum, 8)),
                BaseFee = ParseDecimal(worksheet.Cell(rowNum, 9)) ?? 3.5m,
                AdditionalUnitPrice = ParseDecimal(worksheet.Cell(rowNum, 10)) ?? 0m
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

    private static PriceTableImportResult ParseLaneWorksheet(
        IXLWorksheet worksheet, int headerRow, int dataStartRow, string format, PriceTableImportOptions options)
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

            var siteCode = GetCellText(worksheet, rowNum, 2);
            if (string.IsNullOrWhiteSpace(siteCode))
                siteCode = options.ResolveSiteCode();

            var row = new PriceTableRow
            {
                RowNumber = rowNum,
                EffectiveDate = ParseDate(GetCellText(worksheet, rowNum, 1)),
                SiteCode = siteCode,
                DestCode = string.IsNullOrWhiteSpace(destCode) ? destination.Trim() : destCode.Trim(),
                Destination = string.IsNullOrWhiteSpace(destination) ? destCode.Trim() : destination.Trim(),
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
