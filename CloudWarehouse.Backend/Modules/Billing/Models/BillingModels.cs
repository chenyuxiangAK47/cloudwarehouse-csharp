namespace CloudWarehouse.Backend.Models;

public class CustomerAccount
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int Status { get; set; } = 1;
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public string? Remark { get; set; }
    public string? CustomerName { get; set; }
}

public class BillLine
{
    public long Id { get; set; }
    public string WaybillNo { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public long? CustomerId { get; set; }
    public string? AccountName { get; set; }
    public string? ExpressType { get; set; }
    public string Province { get; set; } = string.Empty;
    public string? City { get; set; }
    public string BillingType { get; set; } = "正向计费";
    public decimal ActualWeight { get; set; }
    public decimal? RoundedWeight { get; set; }
    public decimal? ReceivableTransitFee { get; set; }
    public decimal? ReceivableLabelFee { get; set; }
    public decimal? ReceivableSurcharge { get; set; }
    public decimal? ReceivableTotal { get; set; }
    public decimal? PayableTransitFee { get; set; }
    public decimal? PayableLabelFee { get; set; }
    public decimal? PayableSurcharge { get; set; }
    public decimal? PayableTotal { get; set; }
    public decimal? Profit { get; set; }
    public string? ImportBatchId { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public string? Remark { get; set; }
}
