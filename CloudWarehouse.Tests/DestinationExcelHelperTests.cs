using CloudWarehouse.Backend.Helpers;
using Xunit;

namespace CloudWarehouse.Tests;

public class DestinationExcelHelperTests
{
    [Fact]
    public void ReadDestinations_ParsesTemplateRow()
    {
        using var stream = new MemoryStream(DestinationExcelHelper.CreateImportTemplate());
        var rows = DestinationExcelHelper.ReadDestinations(stream);
        Assert.Single(rows);
        Assert.Equal("11", rows[0].DestCode);
        Assert.Equal("安徽省", rows[0].Province);
    }
}
