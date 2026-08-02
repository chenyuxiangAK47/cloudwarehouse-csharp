using ClosedXML.Excel;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers;

public static class CustomerExcelHelper
{
  private static readonly string[] CodeHeaders = ["CustomerCode", "客户编号", "客户编码"];
  private static readonly string[] NameHeaders = ["CustomerName", "客户名称", "客户名"];

  public static byte[] CreateImportTemplate()
  {
    using var workbook = new XLWorkbook();
    var ws = workbook.AddWorksheet("Customers");
    ws.Cell(1, 1).Value = "客户编号";
    ws.Cell(1, 2).Value = "客户名称";
    ws.Cell(2, 1).Value = "A0001";
    ws.Cell(2, 2).Value = "示例店铺名称";
    ws.Columns().AdjustToContents();
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    return stream.ToArray();
  }

  public static List<CustomerImportRow> ReadCustomers(Stream stream)
  {
    using var workbook = new XLWorkbook(stream);
    var ws = workbook.Worksheets.First();
    var (headerRow, codeCol, nameCol) = FindHeader(ws);
    if (codeCol < 0 || nameCol < 0)
      throw new InvalidOperationException(
        "无法识别表头，请使用第 1 行：客户编号、客户名称（或 CustomerCode / CustomerName）。");

    var rows = new List<CustomerImportRow>();
    var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
    for (var r = headerRow + 1; r <= lastRow; r++)
    {
      var code = GetCell(ws, r, codeCol);
      var name = GetCell(ws, r, nameCol);
      if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name))
        continue;

      rows.Add(new CustomerImportRow
      {
        RowNumber = r,
        CustomerCode = code.Trim(),
        CustomerName = name.Trim()
      });
    }

    if (rows.Count == 0)
      throw new InvalidOperationException("表头下方未找到有效数据行。");

    return rows;
  }

  private static (int headerRow, int codeCol, int nameCol) FindHeader(IXLWorksheet ws)
  {
    var lastRow = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 1, 5);
    for (var r = 1; r <= lastRow; r++)
    {
      var codeCol = -1;
      var nameCol = -1;
      var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 10;
      for (var c = 1; c <= lastCol; c++)
      {
        var text = GetCell(ws, r, c);
        if (CodeHeaders.Any(h => text.Equals(h, StringComparison.OrdinalIgnoreCase)))
          codeCol = c;
        if (NameHeaders.Any(h => text.Equals(h, StringComparison.OrdinalIgnoreCase)))
          nameCol = c;
      }

      if (codeCol > 0 && nameCol > 0)
        return (r, codeCol, nameCol);
    }

    return (1, -1, -1);
  }

  private static string GetCell(IXLWorksheet ws, int row, int col) =>
    ws.Cell(row, col).GetString().Trim();
}
