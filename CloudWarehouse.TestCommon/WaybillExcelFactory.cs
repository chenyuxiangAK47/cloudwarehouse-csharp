using ClosedXML.Excel;

namespace CloudWarehouse.TestCommon;

public static class WaybillExcelFactory
{
    public static MemoryStream CreateStandardWorkbook(
        params Action<IXLWorksheet>[] rowFillers)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("运单明细");

        var headers = new[]
        {
            "账单日期", "运单号", "账户名称", "目的省", "目的市", "结算重量", "快递公司", "附加费", "罚款"
        };

        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        var row = 2;
        foreach (var filler in rowFillers)
        {
            filler(ws);
            row++;
        }

        if (rowFillers.Length == 0)
        {
            FillStandardRow(ws, 2, "YT001", "安徽省", 2.19m);
        }

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    public static void FillStandardRow(
        IXLWorksheet ws, int row, string waybillNo, string province, decimal weight,
        string account = "测试账户", decimal surcharge = 0m)
    {
        ws.Cell(row, 1).Value = new DateTime(2026, 1, 15);
        ws.Cell(row, 2).Value = waybillNo;
        ws.Cell(row, 3).Value = account;
        ws.Cell(row, 4).Value = province;
        ws.Cell(row, 5).Value = "示例市";
        ws.Cell(row, 6).Value = weight;
        ws.Cell(row, 7).Value = "圆通";
        ws.Cell(row, 8).Value = surcharge;
        ws.Cell(row, 9).Value = 0;
    }

    public static MemoryStream CreateSupplierDetailWorkbook()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("账单明细");

        var headers = new[]
        {
            "账单日期", "运单号", "结算对象", "目的省", "目的市", "结算重量", "公斤段",
            "运单使用网点", "面单账号", "中转费/快递费", "附加费", "运费合计", "面单费"
        };

        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        ws.Cell(2, 1).Value = new DateTime(2026, 1, 20);
        ws.Cell(2, 2).Value = "SF20260120001";
        ws.Cell(2, 3).Value = "小二小店";
        ws.Cell(2, 4).Value = "山西省";
        ws.Cell(2, 5).Value = "太原市";
        ws.Cell(2, 6).Value = 1.8;
        ws.Cell(2, 7).Value = 2;
        ws.Cell(2, 8).Value = "默认网点";
        ws.Cell(2, 10).Value = 5.5;
        ws.Cell(2, 11).Value = 1;
        ws.Cell(2, 12).Value = 10;
        ws.Cell(2, 13).Value = 3.5;

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}
