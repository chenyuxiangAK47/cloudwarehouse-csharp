using ClosedXML.Excel;
using CloudWarehouse.Backend.Models;
using System.Globalization;

namespace CloudWarehouse.Backend.Helpers;

public static class CustomerQuoteExcelHelper
{
    private static readonly string[] StandardHeaders =
    [
        "生效时间", "客户编号", "省份", "快递类型",
        "0kg<X<=0.3kg", "0.3kg<X<=0.5kg", "0.5kg<X<=1kg",
        "1kg<X<=2kg", "2kg<X<=3kg", "3kg<X<=4kg", "4kg<X<=5kg",
        "面单费", "续重(元/kg)"
    ];

    public static CustomerQuoteImportResult ReadCustomerQuotes(Stream stream, CustomerQuoteImportOptions? options = null)
    {
        options ??= new CustomerQuoteImportOptions();
        using var workbook = new XLWorkbook(stream);
        var worksheet = ResolveQuoteWorksheet(workbook);

        if (IsWaybillBillDetailSheet(worksheet))
            throw new InvalidOperationException(
                "此文件是「账单明细 / 运单结算」表（含运单号、账单明细、成本明细），不是客户报价。"
                + "请到【运单导入】上传做中转费对比；客户报价请用「93-客户报价-单独.xlsx」或下载本页标准模板。");

        var wideHeaderRow = DetectWideHeaderRow(worksheet);
        if (wideHeaderRow > 0)
            return ParseWideFormat(worksheet, wideHeaderRow, options);

        if (DetectLongFormatHeader(worksheet, out var longHeaderRow))
            return ParseLongFormat(worksheet, longHeaderRow, options);

        throw new InvalidOperationException("无法识别客户报价表头，请使用云仓标准模板或含「公斤段」列的师傅报价表。");
    }

    private static IXLWorksheet ResolveQuoteWorksheet(XLWorkbook workbook)
    {
        if (workbook.Worksheets.Count == 0)
            throw new InvalidOperationException("Excel 中没有工作表。");

        ReadOnlySpan<string> priorities = ["客户价格", "客户报价", "报价表", "报价"];
        foreach (var key in priorities)
        {
            var match = workbook.Worksheets.FirstOrDefault(w =>
                w.Name.Contains(key, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
        }

        foreach (var ws in workbook.Worksheets)
        {
            if (IsWaybillBillDetailSheet(ws))
                return ws;

            if (HasLongFormatDataWithoutHeader(ws))
                return ws;
        }

        throw new InvalidOperationException(
            "未找到客户报价工作表，请确认文件含「_客户价格_ - 报价表」或下载标准模板填写。");
    }

    public static byte[] CreateStandardTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("客户报价");

        for (var c = 0; c < StandardHeaders.Length; c++)
            ws.Cell(1, c + 1).Value = StandardHeaders[c];

        ws.Cell(2, 1).Value = new DateTime(2026, 5, 7);
        ws.Cell(2, 2).Value = "A0001";
        ws.Cell(2, 3).Value = "安徽省";
        ws.Cell(2, 4).Value = "圆通";
        ws.Cell(2, 5).Value = 2.0;
        ws.Cell(2, 6).Value = 2.1;
        ws.Cell(2, 7).Value = 2.5;
        ws.Cell(2, 8).Value = 3.5;
        ws.Cell(2, 9).Value = 4.0;
        ws.Cell(2, 10).Value = 5.0;
        ws.Cell(2, 11).Value = 6.0;
        ws.Cell(2, 12).Value = 3.5;
        ws.Cell(2, 13).Value = 0.8;

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public static byte[] ExportWideResult(IEnumerable<CustomerQuoteTableRow> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("导入结果");

        var headers = new[]
        {
            "行号", "状态", "生效时间", "客户编号", "省份", "快递类型",
            "0-0.3kg", "0.3-0.5kg", "0.5-1kg", "1-2kg", "2-3kg", "3-4kg", "4-5kg",
            "面单费", "续重(元/kg)", "预期(1kg)", "预期(3kg)"
        };

        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        var r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.RowNumber;
            ws.Cell(r, 2).Value = string.IsNullOrEmpty(row.ErrorMessage) ? "成功" : row.ErrorMessage;
            ws.Cell(r, 3).Value = row.EffectiveDate?.ToString("yyyy/M/d") ?? "";
            ws.Cell(r, 4).Value = row.CustomerCode;
            ws.Cell(r, 5).Value = row.Province;
            ws.Cell(r, 6).Value = row.ExpressType ?? "";
            WriteDecimal(ws.Cell(r, 7), row.Price_0_0_3);
            WriteDecimal(ws.Cell(r, 8), row.Price_0_3_0_5);
            WriteDecimal(ws.Cell(r, 9), row.Price_0_5_1);
            WriteDecimal(ws.Cell(r, 10), row.Price_1_2);
            WriteDecimal(ws.Cell(r, 11), row.Price_2_3);
            WriteDecimal(ws.Cell(r, 12), row.Price_3_4);
            WriteDecimal(ws.Cell(r, 13), row.Price_4_5);
            WriteDecimal(ws.Cell(r, 14), row.BaseFee);
            WriteDecimal(ws.Cell(r, 15), row.AdditionalUnitPrice);
            WriteDecimal(ws.Cell(r, 16), row.ExpectedPrice1Kg);
            WriteDecimal(ws.Cell(r, 17), row.ExpectedPrice3Kg);
            r++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static bool DetectLongFormatHeader(IXLWorksheet worksheet, out int headerRow)
    {
        headerRow = 0;
        for (var r = 1; r <= 5; r++)
        {
            var texts = GetRowTexts(worksheet, r);
            if (texts.Any(t => t.Contains("公斤段", StringComparison.OrdinalIgnoreCase))
                && texts.Any(t => t.Contains("省份", StringComparison.OrdinalIgnoreCase) || t == "省"))
            {
                headerRow = r;
                return true;
            }

            if (texts.Any(t => t is "地区" || t.Contains("重量范围", StringComparison.OrdinalIgnoreCase))
                && texts.Any(t => t.Contains("重量范围", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("Kg-Min", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("Kg-Max", StringComparison.OrdinalIgnoreCase)))
            {
                headerRow = r;
                return true;
            }
        }

        if (HasLongFormatDataWithoutHeader(worksheet))
        {
            headerRow = 0;
            return true;
        }

        return false;
    }

    /// <summary>师傅账单明细双行表头（运单结算），不是客户报价。</summary>
    private static bool IsWaybillBillDetailSheet(IXLWorksheet worksheet)
    {
        var row1 = GetRowTexts(worksheet, 1);
        if (row1.Any(t => t.Contains("运单号", StringComparison.OrdinalIgnoreCase))
            && (row1.Any(t => t.Contains("账单明细", StringComparison.OrdinalIgnoreCase))
                || row1.Any(t => t.Contains("成本明细", StringComparison.OrdinalIgnoreCase))))
            return true;

        var row2 = GetRowTexts(worksheet, 2);
        if (row1.Any(t => t.Contains("计费重量", StringComparison.OrdinalIgnoreCase))
            && row2.Count(t => t == "中转费") >= 2)
            return true;

        return false;
    }

    private static bool HasLongFormatDataWithoutHeader(IXLWorksheet worksheet)
    {
        if (IsWaybillBillDetailSheet(worksheet))
            return false;

        var cellA3 = GetCellText(worksheet, 3, 1);
        var cellE3 = GetCellText(worksheet, 3, 5);
        if (string.IsNullOrEmpty(cellA3) || string.IsNullOrEmpty(cellE3))
            return false;

        var bracketText = GetCellText(worksheet, 3, 7);
        var unitPrice = ParseCellDecimal(worksheet.Cell(3, 10));
        if (unitPrice == null)
            return false;

        if (WeightBracketParser.Parse(bracketText) != null)
            return true;

        var maxWeight = ParseDecimal(GetCellText(worksheet, 3, 7));
        var minWeight = ParseDecimal(GetCellText(worksheet, 3, 6));
        return minWeight != null && maxWeight is > 0 and <= 5;
    }

    private static string GetCellText(IXLWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cell(row, col);
        if (cell.DataType == XLDataType.Number)
            return cell.GetDouble().ToString(CultureInfo.InvariantCulture);

        return cell.GetString().Trim();
    }

    private static CustomerQuoteImportResult ParseLongFormat(
        IXLWorksheet worksheet, int headerRow, CustomerQuoteImportOptions options)
    {
        var columnMap = headerRow > 0 ? BuildLongColumnMap(worksheet, headerRow) : CreateDefaultLongColumnMap();
        var dataStartRow = headerRow > 0 ? headerRow + 1 : 3;
        var rows = new List<CustomerQuoteLongRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? dataStartRow;

        for (var r = dataStartRow; r <= lastRow; r++)
        {
            var customerCode = GetCellText(worksheet, r, columnMap.GetValueOrDefault("CustomerCode", 1));
            var province = GetCellText(worksheet, r, columnMap.GetValueOrDefault("Province", 5));
            if (string.IsNullOrEmpty(customerCode) && string.IsNullOrEmpty(province))
                continue;

            var unitPriceCol = columnMap.GetValueOrDefault("UnitPrice", 10);
            var bracketCol = columnMap.GetValueOrDefault("WeightBracket", 7);
            var bracket = GetCellText(worksheet, r, bracketCol);
            if (WeightBracketParser.Parse(bracket) == null)
            {
                var maxCol = columnMap.GetValueOrDefault("MaxWeight", 7);
                var maxW = ParseCellDecimal(worksheet.Cell(r, maxCol));
                if (maxW is > 0 and <= 5)
                    bracket = maxW.Value.ToString(CultureInfo.InvariantCulture);
            }

            var baseFee = ParseDecimal(GetCell(worksheet, r, columnMap, "BaseFee")) ?? options.DefaultBaseFee;
            var versions = headerRow > 0
                ? MasterPriceHistoryHelper.ReadVersions(worksheet, r, MasterPriceHistoryHelper.QuoteHistoryColumns)
                : [];

            if (versions.Count == 0)
            {
                var unitPrice = ParseCellDecimal(worksheet.Cell(r, unitPriceCol));
                if (unitPrice == null)
                    continue;

                rows.Add(new CustomerQuoteLongRow
                {
                    RowNumber = r,
                    CustomerCode = customerCode,
                    CustomerName = NullIfEmpty(GetCell(worksheet, r, columnMap, "CustomerName")),
                    ExpressType = NullIfEmpty(GetCell(worksheet, r, columnMap, "ExpressType")),
                    Province = province,
                    WeightBracket = bracket,
                    UnitPrice = unitPrice.Value,
                    BaseFee = baseFee,
                    EffectiveDate = ParseDate(GetCell(worksheet, r, columnMap, "EffectiveDate"))
                        ?? ParseExcelDate(worksheet, r, columnMap, "EffectiveDate")
                });
                continue;
            }

            foreach (var (effective, expiry) in MasterPriceHistoryHelper.BuildVersionPeriods(versions))
            {
                var price = MasterPriceHistoryHelper.PriceAtDate(versions, effective);
                if (price == null)
                    continue;

                rows.Add(new CustomerQuoteLongRow
                {
                    RowNumber = r,
                    CustomerCode = customerCode,
                    CustomerName = NullIfEmpty(GetCell(worksheet, r, columnMap, "CustomerName")),
                    ExpressType = NullIfEmpty(GetCell(worksheet, r, columnMap, "ExpressType")),
                    Province = province,
                    WeightBracket = bracket,
                    UnitPrice = price.Value,
                    BaseFee = baseFee,
                    EffectiveDate = effective,
                    ExpiryDate = expiry
                });
            }
        }

        return new CustomerQuoteImportResult
        {
            Format = headerRow > 0 ? "师傅公斤段格式" : "师傅报价表(无表头)",
            SheetName = worksheet.Name,
            HeaderRow = headerRow,
            DataStartRow = dataStartRow,
            TotalRows = rows.Count,
            LongRows = rows
        };
    }

    private static Dictionary<string, int> CreateDefaultLongColumnMap() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["CustomerCode"] = 1,
        ["CustomerName"] = 2,
        ["ExpressType"] = 3,
        ["Province"] = 5,
        ["WeightBracket"] = 7,
        ["EffectiveDate"] = 9,
        ["UnitPrice"] = 10
    };

    private static Dictionary<string, int> BuildLongColumnMap(IXLWorksheet worksheet, int headerRow)
    {
        var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["CustomerCode"] = ["客户编号", "编号"],
            ["CustomerName"] = ["客户名称", "客户"],
            ["ExpressType"] = ["快递类型", "快递", "线路"],
            ["Province"] = ["省份", "目的省", "省", "地区"],
            ["WeightBracket"] = ["公斤段", "重量段"],
            ["MinWeight"] = ["Kg-Min", "重量范围\nKg-Min", "最小重量"],
            ["MaxWeight"] = ["Kg-Max", "重量范围\nKg-Max", "最大重量"],
            ["UnitPrice"] = ["中转费", "单价", "报价"],
            ["BaseFee"] = ["面单费"],
            ["EffectiveDate"] = ["生效时间", "生效日期", "更新日期"]
        };

        return BuildColumnMap(worksheet, headerRow, aliases);
    }

    private static int DetectWideHeaderRow(IXLWorksheet worksheet)
    {
        for (var r = 1; r <= 3; r++)
        {
            var match = true;
            for (var c = 0; c < Math.Min(4, StandardHeaders.Length); c++)
            {
                var text = worksheet.Cell(r, c + 1).GetString().Trim();
                if (!string.Equals(text, StandardHeaders[c], StringComparison.OrdinalIgnoreCase))
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return r;
        }

        return 0;
    }

    private static CustomerQuoteImportResult ParseWideFormat(
        IXLWorksheet worksheet, int headerRow, CustomerQuoteImportOptions options)
    {
        var dataStartRow = headerRow + 1;
        var rows = new List<CustomerQuoteTableRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? dataStartRow;

        for (var r = dataStartRow; r <= lastRow; r++)
        {
            var customerCode = worksheet.Cell(r, 2).GetString().Trim();
            var province = worksheet.Cell(r, 3).GetString().Trim();
            if (string.IsNullOrEmpty(customerCode) && string.IsNullOrEmpty(province))
                continue;

            rows.Add(new CustomerQuoteTableRow
            {
                RowNumber = r,
                EffectiveDate = ParseDate(worksheet.Cell(r, 1).GetString()) ?? ParseCellDate(worksheet.Cell(r, 1)),
                CustomerCode = customerCode,
                Province = province,
                ExpressType = NullIfEmpty(worksheet.Cell(r, 4).GetString()),
                Price_0_0_3 = ParseCellDecimal(worksheet.Cell(r, 5)),
                Price_0_3_0_5 = ParseCellDecimal(worksheet.Cell(r, 6)),
                Price_0_5_1 = ParseCellDecimal(worksheet.Cell(r, 7)),
                Price_1_2 = ParseCellDecimal(worksheet.Cell(r, 8)),
                Price_2_3 = ParseCellDecimal(worksheet.Cell(r, 9)),
                Price_3_4 = ParseCellDecimal(worksheet.Cell(r, 10)),
                Price_4_5 = ParseCellDecimal(worksheet.Cell(r, 11)),
                BaseFee = ParseCellDecimal(worksheet.Cell(r, 12)) ?? options.DefaultBaseFee,
                AdditionalUnitPrice = ParseCellDecimal(worksheet.Cell(r, 13)) ?? 0m
            });
        }

        return new CustomerQuoteImportResult
        {
            Format = "标准格式",
            SheetName = worksheet.Name,
            HeaderRow = headerRow,
            DataStartRow = dataStartRow,
            TotalRows = rows.Count,
            WideRows = rows
        };
    }

    private static Dictionary<string, int> BuildColumnMap(
        IXLWorksheet worksheet, int headerRow, Dictionary<string, string[]> aliases)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 20;

        for (var c = 1; c <= lastCol; c++)
        {
            var header = worksheet.Cell(headerRow, c).GetString().Trim();
            if (string.IsNullOrEmpty(header))
                continue;

            foreach (var (field, names) in aliases)
            {
                if (map.ContainsKey(field))
                    continue;

                if (names.Any(n => header.Equals(n, StringComparison.OrdinalIgnoreCase)
                    || (n.Length >= 2 && header.Contains(n, StringComparison.OrdinalIgnoreCase))))
                {
                    map[field] = c;
                    break;
                }
            }
        }

        return map;
    }

    private static List<string> GetRowTexts(IXLWorksheet worksheet, int row)
    {
        var texts = new List<string>();
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 20;
        for (var c = 1; c <= lastCol; c++)
        {
            var t = worksheet.Cell(row, c).GetString().Trim();
            if (!string.IsNullOrEmpty(t))
                texts.Add(t);
        }

        return texts;
    }

    private static string GetCell(IXLWorksheet worksheet, int row, Dictionary<string, int> map, string field)
    {
        if (!map.TryGetValue(field, out var col))
            return string.Empty;

        var cell = worksheet.Cell(row, col);
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime().ToString("yyyy/M/d");

        return cell.GetString().Trim();
    }

    private static DateTime? ParseExcelDate(IXLWorksheet worksheet, int row, Dictionary<string, int> map, string field)
    {
        if (!map.TryGetValue(field, out var col))
            return null;

        return ParseCellDate(worksheet.Cell(row, col));
    }

    private static DateTime? ParseCellDate(IXLCell cell) =>
        cell.DataType == XLDataType.DateTime ? cell.GetDateTime().Date : null;

    private static decimal? ParseCellDecimal(IXLCell cell)
    {
        if (cell.DataType == XLDataType.Number)
            return (decimal)cell.GetDouble();

        return ParseDecimal(cell.GetString());
    }

    private static DateTime? ParseDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.Date;

        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("zh-CN"), DateTimeStyles.None, out dt))
            return dt.Date;

        return null;
    }

    private static decimal? ParseDecimal(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Replace(",", "").Trim();
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("zh-CN"), out v))
            return v;

        return null;
    }

    private static string? NullIfEmpty(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static void WriteDecimal(IXLCell cell, decimal? value)
    {
        if (value.HasValue)
            cell.Value = value.Value;
    }
}
