namespace CloudWarehouse.Backend.Models;

public class PriceTableImportResult
{
    public string SheetName { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public int HeaderRow { get; set; }
    public int DataStartRow { get; set; }
    public int TotalRows { get; set; }
    public int RulesUpserted { get; set; }
    public int SitesCreated { get; set; }
    public int DestinationsCreated { get; set; }
    public int LanesUpserted { get; set; }
    public bool SavedToDatabase { get; set; }
    public List<PriceTableRow> Rows { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    /// <summary>预览时将新建的主数据（未写入库）。</summary>
    public List<string> MasterDataToCreate { get; set; } = [];
}
