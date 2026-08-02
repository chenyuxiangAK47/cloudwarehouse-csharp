namespace CloudWarehouse.Backend.Models;

public class DestinationImportRow
{
    public int RowNumber { get; set; }
    public string DestCode { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class DestinationImportResult
{
    public List<DestinationImportRow> Rows { get; set; } = [];
    public int TotalRows { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public bool SavedToDatabase { get; set; }
}
