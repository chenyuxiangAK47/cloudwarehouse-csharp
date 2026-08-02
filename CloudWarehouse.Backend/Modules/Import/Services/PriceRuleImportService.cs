using System.Data;
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

    public async Task<PriceTableImportResult> ProcessImportAsync(
        Stream stream,
        bool saveToDatabase,
        PriceTableImportOptions? options = null)
    {
        options ??= new PriceTableImportOptions();
        var parsed = ExcelHelper.ReadPriceTable(stream, options);

        try
        {
            if (saveToDatabase)
                await SaveAsync(parsed, options);
            else
                await PreviewResolveAsync(parsed, options);
        }
        catch (SqlException ex) when (!saveToDatabase)
        {
            parsed.Warnings.Add($"未能连接数据库，仅完成 Excel 解析: {ex.Message}");
            ApplyOfflineMasterDataPreview(parsed, options);
            ApplyExpectedPrices(parsed);
        }

        return parsed;
    }

    private async Task PreviewResolveAsync(PriceTableImportResult parsed, PriceTableImportOptions options)
    {
        using var db = new SqlConnection(_conn);
        var siteCache = (await db.QueryAsync<Site>("SELECT Id, SiteCode FROM Sites"))
            .ToDictionary(s => s.SiteCode, StringComparer.OrdinalIgnoreCase);
        var destCache = (await db.QueryAsync<Destination>("SELECT Id, DestCode FROM Destinations"))
            .ToDictionary(d => d.DestCode, StringComparer.OrdinalIgnoreCase);

        var masterPending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in parsed.Rows)
        {
            ValidatePriceRow(row);
            if (!string.IsNullOrEmpty(row.ErrorMessage)) continue;

            ResolveSiteForRow(row, options, siteCache, masterPending, persist: false);
            if (!string.IsNullOrEmpty(row.ErrorMessage)) continue;

            ResolveDestForRow(row, destCache, masterPending, persist: false);
        }

        parsed.MasterDataToCreate.AddRange(masterPending.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        ApplyExpectedPrices(parsed);
    }

    private async Task SaveAsync(PriceTableImportResult parsed, PriceTableImportOptions options)
    {
        ValidateAllRows(parsed);

        using var db = new SqlConnection(_conn);
        await db.OpenAsync();
        using var tx = await db.BeginTransactionAsync();

        try
        {
            var siteCache = (await db.QueryAsync<Site>(
                "SELECT Id, SiteCode FROM Sites", transaction: tx))
                .ToDictionary(s => s.SiteCode, StringComparer.OrdinalIgnoreCase);
            var destCache = (await db.QueryAsync<Destination>(
                "SELECT Id, DestCode FROM Destinations", transaction: tx))
                .ToDictionary(d => d.DestCode, StringComparer.OrdinalIgnoreCase);

            var rulesInserted = 0;
            var lanes = 0;

            foreach (var laneGroup in parsed.Rows.GroupBy(
                r => $"{r.SiteCode.Trim()}\0{r.Destination.Trim()}",
                StringComparer.OrdinalIgnoreCase))
            {
                var firstRow = laneGroup.First();
                var siteId = await EnsureSiteAsync(db, tx, siteCache, firstRow, options, parsed);
                var destId = await EnsureDestinationAsync(db, tx, destCache, firstRow, parsed);

                await db.ExecuteAsync(
                    "DELETE FROM PriceRules WHERE SiteId = @SiteId AND DestId = @DestId",
                    new { SiteId = siteId, DestId = destId }, tx);

                foreach (var row in laneGroup)
                {
                    var rules = PriceRuleMapper.ToPriceRules(row, siteId, destId);
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

                lanes++;
            }

            await tx.CommitAsync();
            parsed.SavedToDatabase = true;
            parsed.RulesUpserted = rulesInserted;
            parsed.LanesUpserted = lanes;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static void ValidateAllRows(PriceTableImportResult parsed)
    {
        foreach (var row in parsed.Rows)
            ValidatePriceRow(row);

        var errors = parsed.Rows.Where(r => !string.IsNullOrEmpty(r.ErrorMessage)).ToList();
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"导入失败，共 {errors.Count} 行有误（未写入数据库）。首条：第 {errors[0].RowNumber} 行 - {errors[0].ErrorMessage}");
    }

    private static void ValidatePriceRow(PriceTableRow row)
    {
        if (string.IsNullOrWhiteSpace(row.SiteCode))
        {
            row.ErrorMessage = "站点编号不能为空（Excel 站点列或导入页默认站点）";
            return;
        }

        var destLabel = string.IsNullOrWhiteSpace(row.DestCode) ? row.Destination : row.DestCode;
        if (string.IsNullOrWhiteSpace(destLabel))
        {
            row.ErrorMessage = "目的地不能为空";
            return;
        }

        if (!row.EffectiveDate.HasValue)
            row.EffectiveDate = DateTime.Today;

        if (!row.Price_0_5_1.HasValue && !row.Price_1_2.HasValue && !row.Price_0_0_3.HasValue)
            row.ErrorMessage = "至少需填写一个 5 公斤以内的区间价格";

        if (row.AdditionalUnitPrice <= 0)
            row.ErrorMessage = "续重单价(>5kg) 必须大于 0";
    }

    private static void ResolveSiteForRow(
        PriceTableRow row,
        PriceTableImportOptions options,
        Dictionary<string, Site> siteCache,
        HashSet<string> masterPending,
        bool persist)
    {
        var code = row.SiteCode.Trim();
        if (siteCache.ContainsKey(code))
        {
            row.SiteId = siteCache[code].Id;
            return;
        }

        if (!persist)
        {
            masterPending.Add($"站点 {code}");
            row.ImportNote = (row.ImportNote ?? "") + $"将新建站点 {code}; ";
        }
    }

    private static void ResolveDestForRow(
        PriceTableRow row,
        Dictionary<string, Destination> destCache,
        HashSet<string> masterPending,
        bool persist)
    {
        var code = NormalizeDestCode(row);
        row.DestCode = code;
        if (string.IsNullOrWhiteSpace(row.Destination))
            row.Destination = code;

        if (destCache.ContainsKey(code))
        {
            row.DestId = destCache[code].Id;
            return;
        }

        if (!persist)
        {
            masterPending.Add($"目的地 {code}");
            row.ImportNote = (row.ImportNote ?? "") + $"将新建目的地 {code}; ";
        }
    }

    private static string NormalizeDestCode(PriceTableRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.DestCode))
            return row.DestCode.Trim();
        return row.Destination.Trim();
    }

    private async Task<long> EnsureSiteAsync(
        SqlConnection db,
        IDbTransaction tx,
        Dictionary<string, Site> cache,
        PriceTableRow row,
        PriceTableImportOptions options,
        PriceTableImportResult parsed)
    {
        var code = row.SiteCode.Trim();
        if (cache.TryGetValue(code, out var existing))
            return existing.Id;

        var id = await db.ExecuteScalarAsync<long>(@"
            INSERT INTO Sites (SiteCode, SiteName, SiteType, ExpressCompany, ContactPerson, ContactPhone, Address, Status, CreateTime)
            OUTPUT INSERTED.Id
            VALUES (@SiteCode, @SiteName, 1, N'', N'', N'', N'', 1, SYSDATETIME())",
            new { SiteCode = code, SiteName = code }, tx);

        cache[code] = new Site { Id = id, SiteCode = code };
        parsed.SitesCreated++;
        return id;
    }

    private async Task<long> EnsureDestinationAsync(
        SqlConnection db,
        IDbTransaction tx,
        Dictionary<string, Destination> cache,
        PriceTableRow row,
        PriceTableImportResult parsed)
    {
        var code = NormalizeDestCode(row);
        row.DestCode = code;
        if (string.IsNullOrWhiteSpace(row.Destination))
            row.Destination = code;

        if (cache.TryGetValue(code, out var existing))
            return existing.Id;

        var id = await db.ExecuteScalarAsync<long>(@"
            INSERT INTO Destinations (DestCode, Province, City, Area, CreateTime)
            OUTPUT INSERTED.Id
            VALUES (@DestCode, @Province, N'', N'', SYSDATETIME())",
            new { DestCode = code, Province = row.Destination.Trim() }, tx);

        cache[code] = new Destination { Id = id, DestCode = code };
        parsed.DestinationsCreated++;
        return id;
    }

    private static void ApplyOfflineMasterDataPreview(
        PriceTableImportResult parsed,
        PriceTableImportOptions options)
    {
        var masterPending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in parsed.Rows)
        {
            ValidatePriceRow(row);
            if (!string.IsNullOrEmpty(row.ErrorMessage)) continue;

            var siteCode = row.SiteCode.Trim();
            if (!string.Equals(siteCode, PriceTableImportOptions.DefaultSiteCode, StringComparison.OrdinalIgnoreCase))
            {
                masterPending.Add($"站点 {siteCode}");
                row.ImportNote = (row.ImportNote ?? "") + $"将新建站点 {siteCode}; ";
            }

            var code = NormalizeDestCode(row);
            row.DestCode = code;
            if (string.IsNullOrWhiteSpace(row.Destination))
                row.Destination = code;

            masterPending.Add($"目的地 {code}");
            row.ImportNote = (row.ImportNote ?? "") + $"将新建目的地 {code}; ";
        }

        parsed.MasterDataToCreate.AddRange(masterPending.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }

    private static void ApplyExpectedPrices(PriceTableImportResult parsed)
    {
        foreach (var row in parsed.Rows)
        {
            if (!string.IsNullOrEmpty(row.ErrorMessage)) continue;
            row.ExpectedPrice1Kg = PriceCalculator.Calculate(row, 1m);
            row.ExpectedPrice5Kg = PriceCalculator.Calculate(row, 5m);
            row.ExpectedPrice10Kg = PriceCalculator.Calculate(row, 10m);
        }
    }
}
