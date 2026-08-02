using CloudWarehouse.Backend.Helpers;
using Xunit;

namespace CloudWarehouse.Tests;

public class CustomerExcelHelperTests
{
    [Fact]
    public void ReadCustomers_ParsesEnglishHeaders()
    {
        using var stream = new MemoryStream(CustomerExcelHelper.CreateImportTemplate());
        var rows = CustomerExcelHelper.ReadCustomers(stream);
        Assert.Single(rows);
        Assert.Equal("A0001", rows[0].CustomerCode);
        Assert.False(string.IsNullOrWhiteSpace(rows[0].CustomerName));
    }
}
