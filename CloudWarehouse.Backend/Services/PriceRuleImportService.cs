using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Services;

public class PriceRuleImportService
{
    private readonly string _conn;

    public PriceRuleImportService(IConfiguration config)
    {
        _conn = config.GetConnectionString("DefaultConnection")!;
    }

    /// <summary>解析 Excel；saveToDatabase=true 时校验并整批事务入库（任一行失败则全部回滚）。</summary>
    public async Task<PriceTableImportResult> ProcessImportAsync(Stream stream, bool saveToDatabase)
    {
        var parsed = ExcelHelper.ReadPriceTable(stream);

        try
        {
            await EnrichAndValidateAsync(parsed);
        }
        catch (SqlException ex) when (!saveToDatabase)
        {
            parsed.Warnings.Add($"未能连接数据库，仅完成 Excel 解析（未校验站点/目的地）: {ex.Message}");
            ApplyExpectedPrices(parsed);
            return parsed;
        }

        if (!saveToDatabase)
            return parsed;

        var errors = parsed.Rows.Where(r => !string.IsNullOrEmpty(r.ErrorMessage)).ToList();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"导入失败，共 {errors.Count} 行有误（未写入数据库）。首条：第 {errors[0].RowNumber} 行 - {errors[0].ErrorMessage}");
        }

        using var db = new SqlConnection(_conn);
        await db.OpenAsync();
        using var tx = await db.BeginTransactionAsync();

        try
        {
            var rulesInserted = 0;
            foreach (var row in parsed.Rows)
            {
                await db.ExecuteAsync(
                    "DELETE FROM PriceRules WHERE SiteId = @SiteId AND DestId = @DestId",
                    new { row.SiteId, row.DestId }, tx);

                var rules = PriceRuleMapper.ToPriceRules(row, row.SiteId!.Value, row.DestId!.Value);
                foreach (var rule in rules)
                {
                    await db.ExecuteAsync(@"
                        INSERT INTO PriceRules (SiteId, DestId, BillingType, MinWeight, MaxWeight, UnitPrice, BaseFee,
                            EffectiveDate, ExpiryDate, Status, CreateTime, Remark)
                        VALUES (@SiteId, @DestId, @BillingType, @MinWeight, @MaxWeight, @UnitPrice, @BaseFee,
                            @EffectiveDate, @ExpiryDate, @Status, @CreateTime, @Remark)",
                        rule, tx);
                    rulesInserted++;
                }
            }

            await tx.CommitAsync();
            parsed.SavedToDatabase = true;
            parsed.RulesUpserted = rulesInserted;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return parsed;
    }

    private async Task EnrichAndValidateAsync(PriceTableImportResult parsed)
    {
        using var db = new SqlConnection(_conn);
        var sites = (await db.QueryAsync<Site>("SELECT Id, SiteCode FROM Sites WHERE Status = 1"))
            .ToDictionary(s => s.SiteCode, StringComparer.OrdinalIgnoreCase);
        var dests = (await db.QueryAsync<Destination>("SELECT Id, DestCode FROM Destinations"))
            .ToDictionary(d => d.DestCode, StringComparer.OrdinalIgnoreCase);

        foreach (var row in parsed.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.SiteCode))
                row.ErrorMessage = "站点编号不能为空";
            else if (!sites.TryGetValue(row.SiteCode.Trim(), out var site))
                row.ErrorMessage = $"站点「{row.SiteCode}」不存在，请先在站点管理中添加";
            else
                row.SiteId = site.Id;

            if (!string.IsNullOrEmpty(row.ErrorMessage)) continue;

            if (string.IsNullOrWhiteSpace(row.DestCode))
                row.ErrorMessage = "目的地代码不能为空";
            else if (!dests.TryGetValue(row.DestCode.Trim(), out var dest))
                row.ErrorMessage = $"目的地/仓库「{row.DestCode}」不存在，请先在目的地管理中添加";
            else
                row.DestId = dest.Id;

            if (!string.IsNullOrEmpty(row.ErrorMessage)) continue;

            if (!row.EffectiveDate.HasValue)
                row.ErrorMessage = "生效时间无效";

            if (!row.Price_0_5_1.HasValue && !row.Price_1_2.HasValue)
                row.ErrorMessage = "至少需填写一个5公斤以内的区间价格";

            if (row.AdditionalUnitPrice <= 0)
                row.ErrorMessage = "续重单价(>5kg)必须大于0";

        }

        ApplyExpectedPrices(parsed);
    }

    private static void ApplyExpectedPrices(PriceTableImportResult parsed)
    {
        foreach (var row in parsed.Rows)
        {
            row.ExpectedPrice1Kg = PriceCalculator.Calculate(row, 1m);
            row.ExpectedPrice5Kg = PriceCalculator.Calculate(row, 5m);
            row.ExpectedPrice10Kg = PriceCalculator.Calculate(row, 10m);
        }
    }
}
