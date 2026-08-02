using ClosedXML.Excel;
using System.Globalization;

namespace CloudWarehouse.Backend.Helpers;

public readonly record struct PriceHistoryVersion(DateTime EffectiveDate, decimal Price);

/// <summary>师傅成本表/客户报价表：历史价格列（更新日期 + 中转费）解析。</summary>
public static class MasterPriceHistoryHelper
{
  public static readonly (int DateCol, int PriceCol)[] CostHistoryColumns =
  [
    (7, 8), (10, 11), (13, 14), (16, 17), (19, 20), (22, 23), (25, 26)
  ];

  public static readonly (int DateCol, int PriceCol)[] QuoteHistoryColumns =
  [
    (9, 10), (12, 13), (15, 16), (18, 19), (21, 22), (24, 25), (27, 28)
  ];

  public static List<PriceHistoryVersion> ReadVersions(
    IXLWorksheet worksheet,
    int row,
    IReadOnlyList<(int DateCol, int PriceCol)> columns)
  {
    var versions = new List<PriceHistoryVersion>();
    foreach (var (dateCol, priceCol) in columns)
    {
      var date = ParseCellDate(worksheet.Cell(row, dateCol));
      var price = ParseDecimal(worksheet.Cell(row, priceCol));
      if (date != null && price != null)
        versions.Add(new PriceHistoryVersion(date.Value, price.Value));
    }

    return versions.OrderBy(v => v.EffectiveDate).ToList();
  }

  public static decimal? PriceAtDate(IReadOnlyList<PriceHistoryVersion> versions, DateTime asOfDate)
  {
    if (versions.Count == 0)
      return null;

    PriceHistoryVersion? match = null;
    foreach (var version in versions)
    {
      if (version.EffectiveDate <= asOfDate.Date)
        match = version;
      else
        break;
    }

    return match?.Price;
  }

  public static List<(DateTime Effective, DateTime? Expiry)> BuildEffectivePeriods(
    IEnumerable<DateTime> versionDates)
  {
    var dates = versionDates.Select(d => d.Date).Distinct().OrderBy(d => d).ToList();
    var periods = new List<(DateTime Effective, DateTime? Expiry)>();
    for (var i = 0; i < dates.Count; i++)
    {
      DateTime? expiry = i < dates.Count - 1 ? dates[i + 1].AddDays(-1) : null;
      periods.Add((dates[i], expiry));
    }

    return periods;
  }

  public static List<(DateTime Effective, DateTime? Expiry)> BuildVersionPeriods(
    IReadOnlyList<PriceHistoryVersion> versions)
  {
    var periods = new List<(DateTime Effective, DateTime? Expiry)>();
    for (var i = 0; i < versions.Count; i++)
    {
      DateTime? expiry = i < versions.Count - 1
        ? versions[i + 1].EffectiveDate.AddDays(-1)
        : null;
      periods.Add((versions[i].EffectiveDate, expiry));
    }

    return periods;
  }

  private static decimal? ParseDecimal(IXLCell cell)
  {
    if (cell.IsEmpty())
      return null;

    if (cell.DataType == XLDataType.Number)
      return (decimal)cell.GetDouble();

    var text = cell.GetFormattedString().Trim();
    if (string.IsNullOrWhiteSpace(text))
      return null;

    text = text.Replace("元", "").Replace("/kg", "").Replace(",", "");
    if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
      return value;
    if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
      return value;

    return null;
  }

  private static DateTime? ParseCellDate(IXLCell cell)
  {
    if (cell.IsEmpty())
      return null;

    if (cell.DataType == XLDataType.DateTime)
      return cell.GetDateTime().Date;

    var text = cell.GetFormattedString().Trim();
    if (string.IsNullOrWhiteSpace(text))
      return null;

    if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var date))
      return date.Date;
    if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
      return date.Date;

    return null;
  }
}
