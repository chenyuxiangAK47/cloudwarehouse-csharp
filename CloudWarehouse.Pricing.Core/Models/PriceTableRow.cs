namespace CloudWarehouse.Backend.Models;

/// <summary>Excel 价格表中的一行（供应商报价）</summary>
public class PriceTableRow
{
    public int RowNumber { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
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

    public long? SiteId { get; set; }
    public long? DestId { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>预览提示：将新建站点/目的地等。</summary>
    public string? ImportNote { get; set; }
}
