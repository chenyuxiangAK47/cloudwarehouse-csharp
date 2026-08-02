using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using CloudWarehouse.Backend.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Services;

public class BillImportService
{
    private readonly string _conn;
    private readonly IDualTrackFeeCalculator _dualTrackFeeCalculator;

    public BillImportService(
        IConfiguration config,
        IDualTrackFeeCalculator dualTrackFeeCalculator)
    {
        _conn = config.GetConnectionString("DefaultConnection")!;
        _dualTrackFeeCalculator = dualTrackFeeCalculator;
    }

    public async Task<WaybillImportResult> ProcessImportAsync(
        Stream stream,
        bool saveToDatabase,
        WaybillImportOptions? options = null)
    {
        options ??= new WaybillImportOptions();
        var parsed = WaybillExcelHelper.ReadWaybills(stream);

        try
        {
            if (saveToDatabase)
                await SaveAsync(parsed, options);
            else
                await PreviewAsync(parsed, options);
        }
        catch (SqlException ex) when (!saveToDatabase)
        {
            parsed.Warnings.Add($"未能连接数据库，仅完成 Excel 解析与重量取整: {ex.Message}");
            ApplyOfflinePreview(parsed, options);
        }

        return parsed;
    }

    private async Task PreviewAsync(WaybillImportResult parsed, WaybillImportOptions options)
    {
        using var db = new SqlConnection(_conn);
        await EnrichAndCalculateAsync(db, parsed, options, persist: false);
    }

    private async Task SaveAsync(WaybillImportResult parsed, WaybillImportOptions options)
    {
        if (parsed.Rows.Count == 0)
            throw new InvalidOperationException("未解析到有效运单数据。");

        using var db = new SqlConnection(_conn);
        await db.OpenAsync();
        using var tx = (SqlTransaction)await db.BeginTransactionAsync();

        try
        {
            await EnrichAndCalculateAsync(db, parsed, options, persist: false, tx);

            var errorCount = parsed.Rows.Count(r => !string.IsNullOrEmpty(r.ErrorMessage));
            if (errorCount > 0)
                throw new InvalidOperationException($"有 {errorCount} 行校验失败，已全部回滚。");

            var batchId = Guid.NewGuid().ToString("N")[..12];
            var saved = 0;

            foreach (var row in parsed.Rows)
            {
                await db.ExecuteAsync(@"
                    MERGE BillLines AS target
                    USING (SELECT @WaybillNo AS WaybillNo) AS source
                    ON target.WaybillNo = source.WaybillNo
                    WHEN MATCHED THEN UPDATE SET
                        BillDate = @BillDate, CustomerId = @CustomerId, AccountName = @AccountName,
                        ExpressType = @ExpressType, Province = @Province, City = @City,
                        BillingType = @BillingType, ActualWeight = @ActualWeight, RoundedWeight = @RoundedWeight,
                        ReceivableTransitFee = @ReceivableTransitFee, ReceivableLabelFee = @ReceivableLabelFee,
                        ReceivableSurcharge = @ReceivableSurcharge, ReceivableTotal = @ReceivableTotal,
                        PayableTransitFee = @PayableTransitFee, PayableLabelFee = @PayableLabelFee,
                        PayableSurcharge = @PayableSurcharge, PayableTotal = @PayableTotal,
                        Profit = @Profit, ImportBatchId = @ImportBatchId, Remark = @Remark
                    WHEN NOT MATCHED THEN INSERT (
                        WaybillNo, BillDate, CustomerId, AccountName, ExpressType, Province, City,
                        BillingType, ActualWeight, RoundedWeight,
                        ReceivableTransitFee, ReceivableLabelFee, ReceivableSurcharge, ReceivableTotal,
                        PayableTransitFee, PayableLabelFee, PayableSurcharge, PayableTotal,
                        Profit, ImportBatchId, Remark
                    ) VALUES (
                        @WaybillNo, @BillDate, @CustomerId, @AccountName, @ExpressType, @Province, @City,
                        @BillingType, @ActualWeight, @RoundedWeight,
                        @ReceivableTransitFee, @ReceivableLabelFee, @ReceivableSurcharge, @ReceivableTotal,
                        @PayableTransitFee, @PayableLabelFee, @PayableSurcharge, @PayableTotal,
                        @Profit, @ImportBatchId, @Remark
                    );",
                    new
                    {
                        row.WaybillNo,
                        BillDate = row.BillDate ?? DateTime.Today,
                        row.CustomerId,
                        row.AccountName,
                        ExpressType = row.ExpressType ?? row.SiteName,
                        row.Province,
                        row.City,
                        BillingType = row.BillingType ?? "正向计费",
                        row.ActualWeight,
                        row.RoundedWeight,
                        row.ReceivableTransitFee,
                        row.ReceivableLabelFee,
                        ReceivableSurcharge = row.Surcharge + row.Penalty,
                        row.ReceivableTotal,
                        row.PayableTransitFee,
                        row.PayableLabelFee,
                        PayableSurcharge = 0m,
                        row.PayableTotal,
                        row.Profit,
                        ImportBatchId = batchId,
                        Remark = (string?)null
                    }, tx);

                saved++;
            }

            await tx.CommitAsync();
            parsed.SavedToDatabase = true;
            parsed.LinesSaved = saved;
            parsed.ImportBatchId = batchId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task EnrichAndCalculateAsync(
        SqlConnection db,
        WaybillImportResult parsed,
        WaybillImportOptions options,
        bool persist,
        SqlTransaction? tx = null)
    {
        var sites = (await db.QueryAsync<Site>(
            "SELECT Id, SiteCode, SiteName, ExpressCompany FROM Sites WHERE Status = 1", transaction: tx))
            .ToList();
        var siteByCode = sites.ToDictionary(s => s.SiteCode, StringComparer.OrdinalIgnoreCase);
        var destinations = (await db.QueryAsync<Destination>(
            "SELECT Id, Province, City FROM Destinations", transaction: tx)).ToList();
        var destRuleCounts = (await db.QueryAsync<(long SiteId, long DestId, int Cnt)>(@"
            SELECT SiteId, DestId, COUNT(*) AS Cnt FROM PriceRules WHERE Status = 1
            GROUP BY SiteId, DestId", transaction: tx))
            .ToDictionary(x => (x.SiteId, x.DestId), x => x.Cnt);
        var accounts = (await db.QueryAsync<CustomerAccount>(@"
            SELECT ca.Id, ca.CustomerId, ca.AccountName
            FROM CustomerAccounts ca WHERE ca.Status = 1", transaction: tx))
            .ToDictionary(a => a.AccountName, StringComparer.OrdinalIgnoreCase);
        var customersByCode = (await db.QueryAsync<Customer>(
            "SELECT Id, CustomerCode, CustomerName FROM Customers WHERE Status = 1", transaction: tx))
            .ToDictionary(c => c.CustomerCode, StringComparer.OrdinalIgnoreCase);

        foreach (var row in parsed.Rows)
        {
            ValidateRow(row);
            if (!string.IsNullOrEmpty(row.ErrorMessage))
                continue;

            ApplyWeightRounding(row);

            if (!string.IsNullOrWhiteSpace(row.CustomerCode)
                && customersByCode.TryGetValue(row.CustomerCode.Trim(), out var customer))
                row.CustomerId = customer.Id;

            if (row.CustomerId == null
                && !string.IsNullOrWhiteSpace(row.AccountName)
                && accounts.TryGetValue(row.AccountName.Trim(), out var account))
                row.CustomerId = account.CustomerId;

            ResolveSite(row, options, siteByCode, sites);
            if (!string.IsNullOrEmpty(row.ErrorMessage))
                continue;

            ResolveDestination(row, destinations, row.SiteId, destRuleCounts);
            if (!string.IsNullOrEmpty(row.ErrorMessage))
                continue;

            await _dualTrackFeeCalculator.CalculateAsync(row);
        }
    }

    private static void ApplyOfflinePreview(WaybillImportResult parsed, WaybillImportOptions options)
    {
        foreach (var row in parsed.Rows)
        {
            ValidateRow(row);
            if (!string.IsNullOrEmpty(row.ErrorMessage))
                continue;

            ApplyWeightRounding(row);
            row.SiteCode = options.ResolveSiteCode();
            row.ErrorMessage = "未连接数据库，无法匹配站点/目的地及计价。";
        }
    }

    private static void ValidateRow(WaybillImportRow row)
    {
        if (string.IsNullOrWhiteSpace(row.WaybillNo))
            row.ErrorMessage = "运单号不能为空";
        else if (string.IsNullOrWhiteSpace(row.Province))
            row.ErrorMessage = "目的省不能为空";
        else if (row.ActualWeight <= 0)
            row.ErrorMessage = "结算重量必须大于 0";
    }

    private static void ApplyWeightRounding(WaybillImportRow row)
    {
        row.WeightBracketCalc = WeightRounding.RoundForForwardBilling(row.ActualWeight);
        row.RoundedWeight = WeightRounding.RoundWeight(row.ActualWeight);
    }

    private static void ResolveSite(
        WaybillImportRow row,
        WaybillImportOptions options,
        Dictionary<string, Site> siteByCode,
        List<Site> sites)
    {
        Site? site = null;

        if (!string.IsNullOrWhiteSpace(row.SiteName))
            site = sites.FirstOrDefault(s =>
                s.SiteName.Equals(row.SiteName, StringComparison.OrdinalIgnoreCase));

        if (site == null && !string.IsNullOrWhiteSpace(row.ExpressType))
        {
            site = sites.FirstOrDefault(s =>
                s.ExpressCompany.Equals(row.ExpressType, StringComparison.OrdinalIgnoreCase)
                || s.SiteName.Contains(row.ExpressType, StringComparison.OrdinalIgnoreCase));
        }

        if (site == null && !string.IsNullOrWhiteSpace(row.ExpressType))
            siteByCode.TryGetValue(row.ExpressType.Trim(), out site);

        if (site == null)
            siteByCode.TryGetValue(options.ResolveSiteCode(), out site);

        if (site == null)
        {
            row.ErrorMessage = $"无法匹配站点（网点/快递：{row.SiteName ?? row.ExpressType ?? "—"}），请先在站点管理维护或使用默认站点 {options.ResolveSiteCode()}";
            return;
        }

        row.SiteId = site.Id;
        row.SiteCode = site.SiteCode;
    }

    private static void ResolveDestination(
        WaybillImportRow row,
        List<Destination> destinations,
        long? siteId,
        Dictionary<(long SiteId, long DestId), int> destRuleCounts)
    {
        var normalized = NormalizeRegion(row.Province);
        var candidates = destinations
            .Where(d => NormalizeRegion(d.Province).Equals(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            row.ErrorMessage = $"目的省「{row.Province}」未在目的地主数据中找到，请先维护目的地。";
            return;
        }

        if (candidates.Count == 1)
        {
            row.DestId = candidates[0].Id;
            return;
        }

        if (siteId != null)
        {
            var best = candidates
                .Select(d => (dest: d, rules: destRuleCounts.GetValueOrDefault((siteId.Value, d.Id))))
                .OrderByDescending(x => x.rules)
                .ThenBy(x => x.dest.Id)
                .First();

            row.DestId = best.dest.Id;
            return;
        }

        row.DestId = candidates.OrderBy(d => d.Id).First().Id;
    }

    public static string NormalizeRegion(string name)
    {
        var text = name.Trim();
        foreach (var suffix in new[] { "维吾尔自治区", "壮族自治区", "回族自治区", "特别行政区", "自治区", "省", "市" })
        {
            if (text.EndsWith(suffix, StringComparison.Ordinal))
            {
                text = text[..^suffix.Length];
                break;
            }
        }

        return text;
    }

}
