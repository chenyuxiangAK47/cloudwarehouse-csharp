using ClosedXML.Excel;
using CloudWarehouse.Backend.Models;
using System.Globalization;

namespace CloudWarehouse.Backend.Helpers;

public static class WaybillExcelHelper
{
    private static readonly string[] StandardHeaders =
    [
        "账单日期", "运单号", "账户名称", "目的省", "目的市", "结算重量", "快递公司",
        "加收-1", "加收-2", "加收-3", "异形件加收", "拦截退改费", "罚款", "赔付", "预付款"
    ];

    private static readonly Dictionary<string, string[]> ColumnAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BillDate"] = ["账单日期", "日期"],
        ["WaybillNo"] = ["运单号", "单号"],
        ["CustomerCode"] = ["客户编号"],
        ["CustomerName"] = ["客户名称"],
        ["AccountName"] = ["结算对象", "账户名称", "面单账号", "客户账户"],
        ["Province"] = ["目的省", "目的地所属省份", "省份", "省"],
        ["City"] = ["目的市", "计费目的地名称", "城市", "市"],
        ["ActualWeight"] = ["结算重量", "计费重量", "重量", "实际重量"],
        ["WeightBracket"] = ["公斤段", "取整", "计费重量段"],
        ["BillingTypeLabel"] = ["计费类型"],
        ["SiteName"] = ["运单使用网点", "收费网点", "网点", "站点"],
        ["ExpressType"] = ["快递公司", "快递", "承运商"],
        ["Surcharge"] = ["附加费"],
        ["Penalty"] = ["罚款", "罚金"],
        ["SourceLabelFee"] = ["面单费"],
        ["SourceTransitFee"] = ["中转费/快递费", "中转费", "快递费"],
        ["SourceTotal"] = ["运费合计", "合计应收", "合计应付", "合计", "总金额"]
    };

    public static WaybillImportResult ReadWaybills(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        if (TryParseBillDetailDualHeader(worksheet, out var dualResult))
            return dualResult;

        worksheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Contains("账单明细", StringComparison.OrdinalIgnoreCase)
            || w.Name.Contains("运单", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var headerRow = DetectHeaderRow(worksheet);
        if (headerRow <= 0)
            throw new InvalidOperationException("无法识别运单表头，请使用云仓标准模板或账单明细双行表头格式。");

        var columnMap = BuildColumnMap(worksheet, headerRow);
        if (!columnMap.ContainsKey("WaybillNo") || !columnMap.ContainsKey("Province") || !columnMap.ContainsKey("ActualWeight"))
            throw new InvalidOperationException("运单表缺少必填列：运单号、目的省、结算重量。");

        var format = IsStandardHeaderRow(worksheet, headerRow) ? "标准格式" : "账单明细格式";
        return ParseSimpleFormat(worksheet, headerRow, headerRow + 1, format, columnMap);
    }

    private static bool TryParseBillDetailDualHeader(IXLWorksheet ws, out WaybillImportResult result)
    {
        result = null!;
        var row1 = GetRowTexts(ws, 1);
        if (!row1.Any(t => t.Contains("账单明细", StringComparison.OrdinalIgnoreCase))
            || !row1.Any(t => t.Contains("成本明细", StringComparison.OrdinalIgnoreCase)))
            return false;

        var row2 = GetRowTexts(ws, 2);
        if (!row2.Any(t => t == "中转费"))
            return false;

        var rows = new List<WaybillImportRow>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 3;
        for (var r = 3; r <= lastRow; r++)
        {
            var waybill = GetCellText(ws, r, 4);
            if (string.IsNullOrWhiteSpace(waybill))
                continue;

            var row = new WaybillImportRow
            {
                RowNumber = r,
                CustomerCode = NullIfEmpty(GetCellText(ws, r, 1)),
                CustomerName = NullIfEmpty(GetCellText(ws, r, 2)),
                ExpressType = NullIfEmpty(GetCellText(ws, r, 3)),
                WaybillNo = waybill.Trim(),
                AccountName = NullIfEmpty(GetCellText(ws, r, 5)),
                BillDate = ParseCellDate(ws.Cell(r, 6)),
                Province = GetCellText(ws, r, 7).Trim(),
                City = NullIfEmpty(GetCellText(ws, r, 8)),
                BillingTypeLabel = NullIfEmpty(GetCellText(ws, r, 9)),
                ActualWeight = ParseCellDecimal(ws.Cell(r, 10)) ?? 0m,
                WeightBracket = NullIfEmpty(GetCellText(ws, r, 11)),
                ExpectedReceivableTransitFee = ParseCellDecimal(ws.Cell(r, 12)),
                ReceivableSurcharge1 = ParseCellDecimal(ws.Cell(r, 13)) ?? 0m,
                ReceivableSurcharge2 = ParseCellDecimal(ws.Cell(r, 14)) ?? 0m,
                ReceivableSurcharge3 = ParseCellDecimal(ws.Cell(r, 15)) ?? 0m,
                ReceivableSpecialSurcharge = ParseCellDecimal(ws.Cell(r, 16)) ?? 0m,
                ReceivableInterceptFee = ParseCellDecimal(ws.Cell(r, 17)) ?? 0m,
                ReceivablePenalty = ParseCellDecimal(ws.Cell(r, 18)) ?? 0m,
                ReceivableCompensation = ParseCellDecimal(ws.Cell(r, 19)) ?? 0m,
                ExpectedReceivableTotal = ParseCellDecimal(ws.Cell(r, 20)),
                ReceivablePrepayment = ParseCellDecimal(ws.Cell(r, 21)),
                ExpectedRemainingReceivable = ParseCellDecimal(ws.Cell(r, 22)),
                ExpectedPayableTransitFee = ParseCellDecimal(ws.Cell(r, 24)),
                PayableSurcharge1 = ParseCellDecimal(ws.Cell(r, 25)) ?? 0m,
                PayableSurcharge2 = ParseCellDecimal(ws.Cell(r, 26)) ?? 0m,
                PayableSurcharge3 = ParseCellDecimal(ws.Cell(r, 27)) ?? 0m,
                PayableSpecialSurcharge = ParseCellDecimal(ws.Cell(r, 28)) ?? 0m,
                PayableInterceptFee = ParseCellDecimal(ws.Cell(r, 29)) ?? 0m,
                PayablePenalty = ParseCellDecimal(ws.Cell(r, 30)) ?? 0m,
                PayableCompensation = ParseCellDecimal(ws.Cell(r, 31)) ?? 0m,
                ExpectedPayableTotal = ParseCellDecimal(ws.Cell(r, 32)),
                PayablePrepayment = ParseCellDecimal(ws.Cell(r, 33)),
                ExpectedRemainingPayable = ParseCellDecimal(ws.Cell(r, 34))
            };

            rows.Add(row);
        }

        result = new WaybillImportResult
        {
            Format = "账单明细双行表头",
            SheetName = ws.Name,
            HeaderRow = 2,
            DataStartRow = 3,
            Rows = rows
        };

        if (rows.Count == 0)
            result.Warnings.Add("未解析到有效数据行。");

        return true;
    }

    private static WaybillImportResult ParseSimpleFormat(
        IXLWorksheet worksheet, int headerRow, int dataStartRow, string format, Dictionary<string, int> columnMap)
    {
        var rows = new List<WaybillImportRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? dataStartRow;

        for (var r = dataStartRow; r <= lastRow; r++)
        {
            if (IsEmptyRow(worksheet, r, columnMap))
                continue;

            rows.Add(new WaybillImportRow
            {
                RowNumber = r,
                BillDate = ParseDate(GetCell(worksheet, r, columnMap, "BillDate"))
                    ?? ParseCellDate(worksheet.Cell(r, columnMap.GetValueOrDefault("BillDate", 1))),
                WaybillNo = GetCell(worksheet, r, columnMap, "WaybillNo").Trim(),
                CustomerCode = NullIfEmpty(GetCell(worksheet, r, columnMap, "CustomerCode")),
                AccountName = NullIfEmpty(GetCell(worksheet, r, columnMap, "AccountName")),
                Province = GetCell(worksheet, r, columnMap, "Province").Trim(),
                City = NullIfEmpty(GetCell(worksheet, r, columnMap, "City")),
                ActualWeight = ParseDecimal(GetCell(worksheet, r, columnMap, "ActualWeight")) ?? 0m,
                WeightBracket = NullIfEmpty(GetCell(worksheet, r, columnMap, "WeightBracket")),
                SiteName = NullIfEmpty(GetCell(worksheet, r, columnMap, "SiteName")),
                ExpressType = NullIfEmpty(GetCell(worksheet, r, columnMap, "ExpressType")),
                Surcharge = ParseDecimal(GetCell(worksheet, r, columnMap, "Surcharge")) ?? 0m,
                Penalty = ParseDecimal(GetCell(worksheet, r, columnMap, "Penalty")) ?? 0m,
                ReceivablePrepayment = ParseDecimal(GetCell(worksheet, r, columnMap, "Prepayment")),
                SourceTransitFee = ParseDecimal(GetCell(worksheet, r, columnMap, "SourceTransitFee")),
                SourceLabelFee = ParseDecimal(GetCell(worksheet, r, columnMap, "SourceLabelFee")),
                SourceTotal = ParseDecimal(GetCell(worksheet, r, columnMap, "SourceTotal"))
            });
        }

        var result = new WaybillImportResult
        {
            Format = format,
            SheetName = worksheet.Name,
            HeaderRow = headerRow,
            DataStartRow = dataStartRow,
            Rows = rows
        };

        if (rows.Count == 0)
            result.Warnings.Add("未解析到有效数据行。");

        return result;
    }

    public static byte[] CreateStandardTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("运单明细");

        for (var c = 0; c < StandardHeaders.Length; c++)
            ws.Cell(1, c + 1).Value = StandardHeaders[c];

        ws.Cell(2, 1).Value = new DateTime(2026, 1, 15);
        ws.Cell(2, 2).Value = "YT202601150001";
        ws.Cell(2, 3).Value = "示例客户账户";
        ws.Cell(2, 4).Value = "安徽省";
        ws.Cell(2, 5).Value = "合肥市";
        ws.Cell(2, 6).Value = 2.19;
        ws.Cell(2, 7).Value = "圆通";
        ws.Cell(2, 15).Value = 4;

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public static byte[] ExportResult(IEnumerable<WaybillImportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("运单结算结果");

        var headers = new[]
        {
            "行号", "状态", "验证", "账单日期", "运单号", "账户名称", "目的省", "目的市",
            "结算重量", "取整重量", "快递公司",
            "应收中转费(算)", "应收中转费(表)", "应收差",
            "应付中转费(算)", "应付中转费(表)", "应付差",
            "合计应收", "预付款", "剩余应收",
            "合计应付", "预付款(成本)", "剩余应付",
            "利润", "备注"
        };

        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        var r = 2;
        foreach (var row in rows)
        {
            var status = row.ErrorMessage
                ?? (row.TransitFeeMatched == true ? "成功" : row.ValidationNote ?? "成功");

            ws.Cell(r, 1).Value = row.RowNumber;
            ws.Cell(r, 2).Value = string.IsNullOrEmpty(row.ErrorMessage) ? "成功" : row.ErrorMessage;
            ws.Cell(r, 3).Value = row.TransitFeeMatched switch
            {
                true => "一致",
                false => "不一致",
                _ => "—"
            };
            ws.Cell(r, 4).Value = row.BillDate?.ToString("yyyy/M/d") ?? "";
            ws.Cell(r, 5).Value = row.WaybillNo;
            ws.Cell(r, 6).Value = row.AccountName ?? "";
            ws.Cell(r, 7).Value = row.Province;
            ws.Cell(r, 8).Value = row.City ?? "";
            WriteDecimal(ws.Cell(r, 9), row.ActualWeight);
            WriteDecimal(ws.Cell(r, 10), row.RoundedWeight);
            ws.Cell(r, 11).Value = row.ExpressType ?? "";
            WriteDecimal(ws.Cell(r, 12), row.ReceivableTransitFee);
            WriteDecimal(ws.Cell(r, 13), row.ExpectedReceivableTransitFee);
            WriteDecimal(ws.Cell(r, 14), row.ReceivableTransitDiff);
            WriteDecimal(ws.Cell(r, 15), row.PayableTransitFee);
            WriteDecimal(ws.Cell(r, 16), row.ExpectedPayableTransitFee);
            WriteDecimal(ws.Cell(r, 17), row.PayableTransitDiff);
            WriteDecimal(ws.Cell(r, 18), row.ReceivableGrandTotal);
            WriteDecimal(ws.Cell(r, 19), row.ReceivablePrepayment);
            WriteDecimal(ws.Cell(r, 20), row.RemainingReceivable);
            WriteDecimal(ws.Cell(r, 21), row.PayableGrandTotal);
            WriteDecimal(ws.Cell(r, 22), row.PayablePrepayment);
            WriteDecimal(ws.Cell(r, 23), row.RemainingPayable);
            WriteDecimal(ws.Cell(r, 24), row.Profit);
            ws.Cell(r, 25).Value = row.ValidationNote ?? "";
            r++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static int DetectHeaderRow(IXLWorksheet worksheet)
    {
        for (var r = 1; r <= 8; r++)
        {
            var texts = GetRowTexts(worksheet, r);
            if (texts.Any(t => t.Contains("运单号", StringComparison.OrdinalIgnoreCase))
                && texts.Any(t => t.Contains("目的", StringComparison.OrdinalIgnoreCase) || t.Contains("省", StringComparison.OrdinalIgnoreCase))
                && texts.Any(t => t.Contains("重量", StringComparison.OrdinalIgnoreCase)))
                return r;
        }

        return IsStandardHeaderRow(worksheet, 1) ? 1 : 0;
    }

    private static bool IsStandardHeaderRow(IXLWorksheet worksheet, int headerRow)
    {
        for (var c = 0; c < Math.Min(7, StandardHeaders.Length); c++)
        {
            var text = worksheet.Cell(headerRow, c + 1).GetString().Trim();
            if (!string.Equals(text, StandardHeaders[c], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static Dictionary<string, int> BuildColumnMap(IXLWorksheet worksheet, int headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var aliases = new Dictionary<string, string[]>(ColumnAliases)
        {
            ["Prepayment"] = ["预付款"]
        };

        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 30;
        for (var c = 1; c <= lastCol; c++)
        {
            var header = worksheet.Cell(headerRow, c).GetString().Trim();
            if (string.IsNullOrEmpty(header))
                continue;

            foreach (var (field, names) in aliases)
            {
                if (map.ContainsKey(field))
                    continue;

                if (names.Any(a => header.Equals(a, StringComparison.OrdinalIgnoreCase)))
                {
                    map[field] = c;
                    break;
                }

                if (names.Any(a => a.Length >= 3 && header.Contains(a, StringComparison.OrdinalIgnoreCase)))
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
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 40;
        for (var c = 1; c <= lastCol; c++)
        {
            var t = worksheet.Cell(row, c).GetString().Trim();
            if (!string.IsNullOrEmpty(t))
                texts.Add(t);
        }

        return texts;
    }

    private static bool IsEmptyRow(IXLWorksheet worksheet, int row, Dictionary<string, int> columnMap)
    {
        if (columnMap.TryGetValue("WaybillNo", out var waybillCol))
        {
            var waybill = GetCellText(worksheet, row, waybillCol);
            if (!string.IsNullOrEmpty(waybill))
                return false;
        }

        if (columnMap.TryGetValue("Province", out var provinceCol))
        {
            var province = GetCellText(worksheet, row, provinceCol);
            if (!string.IsNullOrEmpty(province))
                return false;
        }

        return true;
    }

    private static string GetCell(IXLWorksheet worksheet, int row, Dictionary<string, int> columnMap, string field)
    {
        if (!columnMap.TryGetValue(field, out var col))
            return string.Empty;

        return GetCellText(worksheet, row, col);
    }

    private static string GetCellText(IXLWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cell(row, col);
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime().ToString("yyyy/M/d");

        if (cell.DataType == XLDataType.Number)
            return cell.GetDouble().ToString(CultureInfo.InvariantCulture);

        return cell.GetString().Trim();
    }

    private static DateTime? ParseCellDate(IXLCell cell) =>
        cell.DataType == XLDataType.DateTime ? cell.GetDateTime().Date : ParseDate(cell.GetString());

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
