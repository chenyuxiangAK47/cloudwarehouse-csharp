using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers;

public static class BillLineTotals
{
    public static decimal SumReceivableExtras(WaybillImportRow row) =>
        row.ReceivableSurcharge1 + row.ReceivableSurcharge2 + row.ReceivableSurcharge3
        + row.ReceivableSpecialSurcharge + row.ReceivableInterceptFee
        + row.ReceivablePenalty + row.ReceivableCompensation
        + row.Surcharge + row.Penalty;

    public static decimal SumPayableExtras(WaybillImportRow row) =>
        row.PayableSurcharge1 + row.PayableSurcharge2 + row.PayableSurcharge3
        + row.PayableSpecialSurcharge + row.PayableInterceptFee
        + row.PayablePenalty + row.PayableCompensation;

    /// <summary>合计应收 = 中转费 + 各项加收/罚款/赔付（不含预付款；与师傅表一致）。</summary>
    public static decimal CalcReceivableGrandTotal(WaybillImportRow row) =>
        (row.ReceivableTransitFee ?? 0m) + SumReceivableExtras(row);

    /// <summary>合计应付 = 应付中转费 + 各项加收/罚款/赔付。</summary>
    public static decimal CalcPayableGrandTotal(WaybillImportRow row) =>
        (row.PayableTransitFee ?? 0m) + SumPayableExtras(row);

    /// <summary>剩余应收 = 合计应收 - 预付款。</summary>
    public static decimal? CalcRemainingReceivable(WaybillImportRow row)
    {
        if (row.ReceivablePrepayment == null)
            return null;

        return Math.Round(CalcReceivableGrandTotal(row) - row.ReceivablePrepayment.Value, 2);
    }

    /// <summary>剩余应付 = 合计应付 + 预付款（师傅表预付款为负表示抵扣）。</summary>
    public static decimal? CalcRemainingPayable(WaybillImportRow row)
    {
        if (row.PayablePrepayment == null)
            return null;

        return Math.Round(CalcPayableGrandTotal(row) + row.PayablePrepayment.Value, 2);
    }

    public static void ApplyComparison(WaybillImportRow row, decimal tolerance = 0.01m)
    {
        if (row.ReceivableTransitFee.HasValue && row.ExpectedReceivableTransitFee.HasValue)
        {
            row.ReceivableTransitDiff = Math.Round(
                row.ReceivableTransitFee.Value - row.ExpectedReceivableTransitFee.Value, 2);
        }

        if (row.PayableTransitFee.HasValue && row.ExpectedPayableTransitFee.HasValue)
        {
            row.PayableTransitDiff = Math.Round(
                row.PayableTransitFee.Value - row.ExpectedPayableTransitFee.Value, 2);
        }

        var recvOk = !row.ExpectedReceivableTransitFee.HasValue
            || (row.ReceivableTransitDiff.HasValue && Math.Abs(row.ReceivableTransitDiff.Value) <= tolerance);
        var payOk = !row.ExpectedPayableTransitFee.HasValue
            || (row.PayableTransitDiff.HasValue && Math.Abs(row.PayableTransitDiff.Value) <= tolerance);

        if (row.ExpectedReceivableTransitFee.HasValue || row.ExpectedPayableTransitFee.HasValue)
        {
            row.TransitFeeMatched = recvOk && payOk;
            row.ValidationNote = row.TransitFeeMatched == true
                ? "中转费对比一致"
                : BuildMismatchNote(row);
        }
    }

    private static string BuildMismatchNote(WaybillImportRow row)
    {
        var parts = new List<string>();
        if (row.ReceivableTransitDiff.HasValue && Math.Abs(row.ReceivableTransitDiff.Value) > 0.01m)
            parts.Add($"应收差{row.ReceivableTransitDiff:0.##}");
        if (row.PayableTransitDiff.HasValue && Math.Abs(row.PayableTransitDiff.Value) > 0.01m)
            parts.Add($"应付差{row.PayableTransitDiff:0.##}");
        return parts.Count > 0 ? "中转费对比不一致: " + string.Join(", ", parts) : "中转费对比不一致";
    }
}
