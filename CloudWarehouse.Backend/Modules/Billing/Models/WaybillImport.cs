namespace CloudWarehouse.Backend.Models;

public class WaybillImportOptions
{
    public const string DefaultSiteCode = "C001";

    public string? SiteCodeOverride { get; set; }

    public string ResolveSiteCode() =>
        string.IsNullOrWhiteSpace(SiteCodeOverride) ? DefaultSiteCode : SiteCodeOverride.Trim();
}

public class WaybillImportRow
{
    public int RowNumber { get; set; }
    public DateTime? BillDate { get; set; }
    public string WaybillNo { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? AccountName { get; set; }
    public string Province { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? BillingTypeLabel { get; set; }
    public decimal ActualWeight { get; set; }
    public string? WeightBracket { get; set; }
    public string? ExpressType { get; set; }
    public string? SiteName { get; set; }
    public decimal Surcharge { get; set; }
    public decimal Penalty { get; set; }
    public decimal? SourceTransitFee { get; set; }
    public decimal? SourceLabelFee { get; set; }
    public decimal? SourceTotal { get; set; }

    // 账单明细（应收侧导入字段）
    public decimal ReceivableSurcharge1 { get; set; }
    public decimal ReceivableSurcharge2 { get; set; }
    public decimal ReceivableSurcharge3 { get; set; }
    public decimal ReceivableSpecialSurcharge { get; set; }
    public decimal ReceivableInterceptFee { get; set; }
    public decimal ReceivablePenalty { get; set; }
    public decimal ReceivableCompensation { get; set; }
    public decimal? ReceivablePrepayment { get; set; }

    // 成本明细（应付侧导入字段）
    public decimal PayableSurcharge1 { get; set; }
    public decimal PayableSurcharge2 { get; set; }
    public decimal PayableSurcharge3 { get; set; }
    public decimal PayableSpecialSurcharge { get; set; }
    public decimal PayableInterceptFee { get; set; }
    public decimal PayablePenalty { get; set; }
    public decimal PayableCompensation { get; set; }
    public decimal? PayablePrepayment { get; set; }

    // 客户表内人工核算值（仅用于对比验证）
    public decimal? ExpectedReceivableTransitFee { get; set; }
    public decimal? ExpectedPayableTransitFee { get; set; }
    public decimal? ExpectedReceivableTotal { get; set; }
    public decimal? ExpectedPayableTotal { get; set; }
    public decimal? ExpectedRemainingReceivable { get; set; }
    public decimal? ExpectedRemainingPayable { get; set; }

    public long? CustomerId { get; set; }
    public long? SiteId { get; set; }
    public string? SiteCode { get; set; }
    public long? DestId { get; set; }

    public decimal? RoundedWeight { get; set; }
    public string? WeightBracketCalc { get; set; }
    public string? BillingType { get; set; }
    public decimal? ReceivableTransitFee { get; set; }
    public decimal? ReceivableLabelFee { get; set; }
    public decimal? ReceivableGrandTotal { get; set; }
    public decimal? RemainingReceivable { get; set; }
    public decimal? ReceivableTotal { get; set; }
    public decimal? PayableTransitFee { get; set; }
    public decimal? PayableLabelFee { get; set; }
    public decimal? PayableGrandTotal { get; set; }
    public decimal? RemainingPayable { get; set; }
    public decimal? PayableTotal { get; set; }
    public decimal? Profit { get; set; }

    public decimal? ReceivableTransitDiff { get; set; }
    public decimal? PayableTransitDiff { get; set; }
    public bool? TransitFeeMatched { get; set; }
    public string? ValidationNote { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WaybillImportResult
{
    public string Format { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public int HeaderRow { get; set; }
    public int DataStartRow { get; set; }
    public List<WaybillImportRow> Rows { get; set; } = [];
    public int TotalRows => Rows.Count;
    public int MatchedTransitRows => Rows.Count(r => r.TransitFeeMatched == true);
    public int MismatchTransitRows => Rows.Count(r => r.TransitFeeMatched == false);
    public bool SavedToDatabase { get; set; }
    public int LinesSaved { get; set; }
    public string? ImportBatchId { get; set; }
    public List<string> Warnings { get; set; } = [];
}
