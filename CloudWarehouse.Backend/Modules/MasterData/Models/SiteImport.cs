namespace CloudWarehouse.Backend.Models;

public class SiteImportRow
{
    public int RowNumber { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public int SiteType { get; set; } = 1;
    public string ExpressCompany { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Status { get; set; } = 1;
    public string? Remark { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SiteImportResult
{
    public List<SiteImportRow> Rows { get; set; } = [];
    public int TotalRows { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public bool SavedToDatabase { get; set; }
}
