namespace CloudWarehouse.Backend.Models;

public class Customer
{
    public long Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Status { get; set; } = 1;
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public string? Remark { get; set; }
}

public class CustomerImportRow
{
    public int RowNumber { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class CustomerImportResult
{
    public List<CustomerImportRow> Rows { get; set; } = [];
    public int TotalRows { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public bool SavedToDatabase { get; set; }
}
