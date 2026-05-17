namespace CloudWarehouse.Backend.Models;

public class Destination
{
    public long Id { get; set; }
    public string DestCode { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; } = DateTime.Now;
}