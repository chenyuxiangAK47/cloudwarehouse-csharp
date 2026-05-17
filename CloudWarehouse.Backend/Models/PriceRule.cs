namespace CloudWarehouse.Backend.Models;

public class PriceRule
{
    public long Id { get; set; }
    public long SiteId { get; set; }
    public long DestId { get; set; }
    public int BillingType { get; set; }
    public decimal MinWeight { get; set; }
    public decimal MaxWeight { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal BaseFee { get; set; } = 3.5m;
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int Status { get; set; } = 1;
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public string? Remark { get; set; }
}

public class PriceCalculateRequest
{
    public long SiteId { get; set; }
    public long DestId { get; set; }
    public decimal Weight { get; set; }
    public DateTime? OrderDate { get; set; }
}

public class PriceCalculateResult
{
    public decimal TotalPrice { get; set; }
    public decimal BaseFee { get; set; }
    public decimal WeightFee { get; set; }
    public string BillingType { get; set; } = string.Empty;
}