using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Tests;

public class BillLineTotalsTests
{
    [Fact]
    public void CalcGrandTotals_SumsTransitAndExtras()
    {
        var row = new WaybillImportRow
        {
            ReceivableTransitFee = 2.7m,
            ReceivableSurcharge1 = 1m,
            PayableTransitFee = 1.5m,
            PayablePenalty = 0.5m
        };

        Assert.Equal(3.7m, BillLineTotals.CalcReceivableGrandTotal(row));
        Assert.Equal(2m, BillLineTotals.CalcPayableGrandTotal(row));
    }

    [Fact]
    public void CalcRemaining_UsesPrepaymentSignsFromMasterSheet()
    {
        var row = new WaybillImportRow
        {
            ReceivableTransitFee = 2.7m,
            ReceivablePrepayment = 4m,
            PayableTransitFee = 1.5m,
            PayablePrepayment = -2.5m
        };

        Assert.Equal(-1.3m, BillLineTotals.CalcRemainingReceivable(row));
        Assert.Equal(-1m, BillLineTotals.CalcRemainingPayable(row));
    }

    [Fact]
    public void ApplyComparison_FlagsMismatchWithinTolerance()
    {
        var matched = new WaybillImportRow
        {
            ReceivableTransitFee = 2.705m,
            ExpectedReceivableTransitFee = 2.7m,
            PayableTransitFee = 1.5m,
            ExpectedPayableTransitFee = 1.5m
        };
        BillLineTotals.ApplyComparison(matched);
        Assert.True(matched.TransitFeeMatched);
        Assert.Equal("中转费对比一致", matched.ValidationNote);

        var mismatched = new WaybillImportRow
        {
            ReceivableTransitFee = 3m,
            ExpectedReceivableTransitFee = 2.7m,
            PayableTransitFee = 1.5m,
            ExpectedPayableTransitFee = 1.5m
        };
        BillLineTotals.ApplyComparison(mismatched);
        Assert.False(mismatched.TransitFeeMatched);
        Assert.Contains("应收差", mismatched.ValidationNote);
    }
}
