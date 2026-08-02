using CloudWarehouse.Backend.Helpers;
using Xunit;

namespace CloudWarehouse.Tests;

public class SiteExcelHelperTests
{
    [Fact]
    public void ReadSites_ParsesTemplateRow()
    {
        using var stream = new MemoryStream(SiteExcelHelper.CreateImportTemplate());
        var rows = SiteExcelHelper.ReadSites(stream);
        Assert.Single(rows);
        Assert.Equal("C001", rows[0].SiteCode);
        Assert.Equal("石家庄配送站", rows[0].SiteName);
        Assert.Equal(1, rows[0].SiteType);
    }
}
