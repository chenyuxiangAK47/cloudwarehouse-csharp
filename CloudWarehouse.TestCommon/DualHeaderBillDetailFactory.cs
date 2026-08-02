using ClosedXML.Excel;

namespace CloudWarehouse.TestCommon;

public static class DualHeaderBillDetailFactory
{
    public static MemoryStream CreateSampleWorkbook()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("2026-01 - 账单明细");

        ws.Cell(1, 1).Value = "客户编号";
        ws.Cell(1, 2).Value = "客户名称";
        ws.Cell(1, 3).Value = "快递公司";
        ws.Cell(1, 4).Value = "运单号";
        ws.Cell(1, 5).Value = "结算对象";
        ws.Cell(1, 6).Value = "账单日期";
        ws.Cell(1, 7).Value = "目的省";
        ws.Cell(1, 8).Value = "目的市";
        ws.Cell(1, 9).Value = "计费类型";
        ws.Cell(1, 10).Value = "计费重量";
        ws.Cell(1, 11).Value = "取整";
        ws.Cell(1, 12).Value = "账单明细";
        ws.Cell(1, 24).Value = "成本明细";

        ws.Cell(2, 12).Value = "中转费";
        ws.Cell(2, 13).Value = "加收-1";
        ws.Cell(2, 14).Value = "加收-2";
        ws.Cell(2, 15).Value = "加收-3";
        ws.Cell(2, 16).Value = "异形件加收";
        ws.Cell(2, 17).Value = "拦截退改费";
        ws.Cell(2, 18).Value = "罚款";
        ws.Cell(2, 19).Value = "赔付";
        ws.Cell(2, 20).Value = "合计应收";
        ws.Cell(2, 21).Value = "预付款";
        ws.Cell(2, 22).Value = "剩余应收";
        ws.Cell(2, 24).Value = "中转费";
        ws.Cell(2, 25).Value = "加收-1";
        ws.Cell(2, 32).Value = "合计应付";
        ws.Cell(2, 33).Value = "预付款";
        ws.Cell(2, 34).Value = "剩余应付";

        ws.Cell(3, 1).Value = "93";
        ws.Cell(3, 2).Value = "小二小店";
        ws.Cell(3, 3).Value = "圆通";
        ws.Cell(3, 4).Value = "YT20260101001";
        ws.Cell(3, 5).Value = "测试账户";
        ws.Cell(3, 6).Value = new DateTime(2026, 1, 5);
        ws.Cell(3, 7).Value = "云南省";
        ws.Cell(3, 8).Value = "昆明市";
        ws.Cell(3, 10).Value = 0.1;
        ws.Cell(3, 11).Value = 0.3;
        ws.Cell(3, 12).Value = 2.7;
        ws.Cell(3, 21).Value = 4;
        ws.Cell(3, 24).Value = 1.5;
        ws.Cell(3, 33).Value = -2.5;

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}
