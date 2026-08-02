using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.IntegrationTests;

/// <summary>
/// GitHub Actions 无 SQL Server：依赖库的集成测试应跳过，而不是失败。
/// </summary>
public static class DatabaseAvailability
{
    public static bool IsUnavailable<T>(ApiResponse<T>? body)
    {
        if (body == null)
            return true;

        if (!body.Success && LooksLikeSqlOutage(body.Message))
            return true;

        if (body.Data is CustomerQuoteImportResult quote
            && quote.Warnings.Any(LooksLikeSqlOutage))
            return true;

        if (body.Data is PriceTableImportResult price
            && price.Warnings.Any(LooksLikeSqlOutage))
            return true;

        if (body.Data is WaybillImportResult waybill
            && waybill.Warnings.Any(LooksLikeSqlOutage))
            return true;

        return false;
    }

    public static bool LooksLikeSqlOutage(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Contains("未能连接数据库", StringComparison.Ordinal)
            || text.Contains("数据库错误", StringComparison.Ordinal)
            || text.Contains("Could not open a connection to SQL Server", StringComparison.OrdinalIgnoreCase)
            || text.Contains("server was not found or was not accessible", StringComparison.OrdinalIgnoreCase)
            || text.Contains("network-related or instance-specific error", StringComparison.OrdinalIgnoreCase);
    }
}
