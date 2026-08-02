using ClosedXML.Excel;

namespace CloudWarehouse.TestCommon;

public static class CustomerQuoteExcelFactory
{
    public static MemoryStream CreateStandardWideWorkbook(
        string customerCode = "A0001",
        string province = "jiangxi",
        decimal tier2to3 = 9.9m,
        decimal baseFee = 4m)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("客户报价");

        var headers = new[]
        {
            "生效时间", "客户编号", "省份", "快递类型",
            "0kg<X<=0.3kg", "0.3kg<X<=0.5kg", "0.5kg<X<=1kg",
            "1kg<X<=2kg", "2kg<X<=3kg", "3kg<X<=4kg", "4kg<X<=5kg",
            "面单费", "续重(元/kg)"
        };

        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        ws.Cell(2, 1).Value = new DateTime(2026, 5, 7);
        ws.Cell(2, 2).Value = customerCode;
        ws.Cell(2, 3).Value = province;
        ws.Cell(2, 4).Value = "圆通";
        ws.Cell(2, 5).Value = 2m;
        ws.Cell(2, 6).Value = 2m;
        ws.Cell(2, 7).Value = 2.5m;
        ws.Cell(2, 8).Value = 3.5m;
        ws.Cell(2, 9).Value = tier2to3;
        ws.Cell(2, 10).Value = 6m;
        ws.Cell(2, 11).Value = 7m;
        ws.Cell(2, 12).Value = baseFee;
        ws.Cell(2, 13).Value = 1m;

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}
