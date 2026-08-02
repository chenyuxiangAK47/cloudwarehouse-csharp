namespace CloudWarehouse.Backend.Models;

public class CustomerQuoteRule
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Province { get; set; } = string.Empty;
    public string? ExpressType { get; set; }
    public int BillingType { get; set; }
    public decimal MinWeight { get; set; }
    public decimal MaxWeight { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal BaseFee { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int Status { get; set; } = 1;
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public string? Remark { get; set; }
}

public class CustomerQuoteTableRow
{
    public int RowNumber { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string? ExpressType { get; set; }

    public decimal? Price_0_0_3 { get; set; }
    public decimal? Price_0_3_0_5 { get; set; }
    public decimal? Price_0_5_1 { get; set; }
    public decimal? Price_1_2 { get; set; }
    public decimal? Price_2_3 { get; set; }
    public decimal? Price_3_4 { get; set; }
    public decimal? Price_4_5 { get; set; }

    public decimal BaseFee { get; set; }
    public decimal AdditionalUnitPrice { get; set; }

    public decimal? ExpectedPrice1Kg { get; set; }
    public decimal? ExpectedPrice3Kg { get; set; }

    public long? CustomerId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ImportNote { get; set; }
}

public class CustomerQuoteLongRow
{
    public int RowNumber { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? ExpressType { get; set; }
    public string Province { get; set; } = string.Empty;
    public string WeightBracket { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal BaseFee { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public long? CustomerId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CustomerQuoteImportResult
{
    public string SheetName { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public int HeaderRow { get; set; }
    public int DataStartRow { get; set; }
    public int TotalRows { get; set; }
    public int RulesUpserted { get; set; }
    public int LanesUpserted { get; set; }
    public bool SavedToDatabase { get; set; }
    public List<CustomerQuoteTableRow> WideRows { get; set; } = [];
    public List<CustomerQuoteLongRow> LongRows { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> MasterDataToCreate { get; set; } = [];
}

public class CustomerQuoteCalculateRequest
{
    public long CustomerId { get; set; }
    public string Province { get; set; } = string.Empty;
    public string? ExpressType { get; set; }
    public decimal Weight { get; set; }
    public DateTime? OrderDate { get; set; }
}

public class CustomerQuoteImportOptions
{
    public decimal DefaultBaseFee { get; set; }
}
