using ClosedXML.Excel;

namespace CloudWarehouse.Tests.Helpers;

/// <summary>在内存中构造三级表头价格表，用于 Excel 解析单测。</summary>
internal static class PriceTableExcelFactory
{
    public static MemoryStream CreateValidWorkbook(params Action<IXLWorksheet>[] dataRowConfigurators)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("价格表");

        ws.Cell(1, 4).Value = "价格表";
        ws.Cell(3, 1).Value = "生效时间";
        ws.Cell(3, 2).Value = "站点编号";
        ws.Cell(3, 3).Value = "目的地代码";
        ws.Cell(3, 4).Value = "目的地";
        ws.Cell(3, 5).Value = "0kg<X<=0.3kg";
        ws.Cell(3, 6).Value = "0.3kg<X<=0.5kg";
        ws.Cell(3, 7).Value = "0.5kg<X<=1kg";
        ws.Cell(3, 8).Value = "1kg<X<=2kg";
        ws.Cell(3, 9).Value = "2kg<X<=3kg";
        ws.Cell(3, 10).Value = "3kg<X<=4kg";
        ws.Cell(3, 11).Value = "4kg<X<=5kg";
        ws.Cell(3, 12).Value = "面单费";
        ws.Cell(3, 13).Value = "续重(元/kg)";

        int row = 4;
        foreach (var configure in dataRowConfigurators)
        {
            configure(ws);
            row++;
        }

        if (dataRowConfigurators.Length == 0)
        {
            FillSampleRow(ws, 4, "11", "安徽省");
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    public static void FillSampleRow(IXLWorksheet ws, int row, string destCode, string destination)
    {
        ws.Cell(row, 1).Value = new DateTime(2026, 5, 7);
        ws.Cell(row, 2).Value = "C001";
        ws.Cell(row, 3).Value = destCode;
        ws.Cell(row, 4).Value = destination;
        ws.Cell(row, 5).Value = 1.6;
        ws.Cell(row, 6).Value = 1.7;
        ws.Cell(row, 7).Value = 2.1;
        ws.Cell(row, 8).Value = 3.3;
        ws.Cell(row, 9).Value = 3.9;
        ws.Cell(row, 10).Value = 5;
        ws.Cell(row, 11).Value = 6;
        ws.Cell(row, 12).Value = 3.5;
        ws.Cell(row, 13).Value = 0.7;
    }

    public static MemoryStream CreateInvalidHeaderWorkbook()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("价格表");
        ws.Cell(3, 1).Value = "错误表头";

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
