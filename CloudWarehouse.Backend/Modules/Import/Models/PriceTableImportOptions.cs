namespace CloudWarehouse.Backend.Models;

/// <summary>价格表导入参数。矩阵表无站点列时使用系统默认站点 C001。</summary>
public class PriceTableImportOptions
{
    /// <summary>系统默认发货站点（矩阵价目表、Excel 站点列为空时使用）。</summary>
    public const string DefaultSiteCode = "C001";

    /// <summary>可选覆盖默认站点（一般留空即可）。</summary>
    public string? SiteCodeOverride { get; set; }

    public string ResolveSiteCode() =>
        string.IsNullOrWhiteSpace(SiteCodeOverride) ? DefaultSiteCode : SiteCodeOverride.Trim();
}
