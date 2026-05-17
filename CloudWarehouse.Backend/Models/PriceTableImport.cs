namespace CloudWarehouse.Backend.Models;

/// <summary>Excel 价格表中的一行（供应商报价）</summary>
public class PriceTableRow
{
    public int RowNumber { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public string DestCode { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;

    public decimal? Price_0_0_3 { get; set; }
    public decimal? Price_0_3_0_5 { get; set; }
    public decimal? Price_0_5_1 { get; set; }
    public decimal? Price_1_2 { get; set; }
    public decimal? Price_2_3 { get; set; }
    public decimal? Price_3_4 { get; set; }
    public decimal? Price_4_5 { get; set; }

    public decimal BaseFee { get; set; } = 3.5m;
    public decimal AdditionalUnitPrice { get; set; }

    /// <summary>按规则试算：1kg 预期总价（区间价 + 面单费）</summary>
    public decimal? ExpectedPrice1Kg { get; set; }
    /// <summary>按规则试算：5kg 预期总价</summary>
    public decimal? ExpectedPrice5Kg { get; set; }
    /// <summary>按规则试算：10kg 预期总价（面单费 + 续重）</summary>
    public decimal? ExpectedPrice10Kg { get; set; }
}

public class PriceTableImportResult
{
    public string SheetName { get; set; } = string.Empty;
    public int HeaderRow { get; set; }
    public int DataStartRow { get; set; }
    public int TotalRows { get; set; }
    public List<PriceTableRow> Rows { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
