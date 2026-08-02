using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Services;

public class CustomerQuoteImportService
{
    private readonly string _conn;

    public CustomerQuoteImportService(IConfiguration config)
    {
        _conn = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task<CustomerQuoteImportResult> ProcessImportAsync(
        Stream stream,
        bool saveToDatabase,
        CustomerQuoteImportOptions? options = null)
    {
        options ??= new CustomerQuoteImportOptions();
        var parsed = CustomerQuoteExcelHelper.ReadCustomerQuotes(stream, options);

        try
        {
            if (saveToDatabase)
                await SaveAsync(parsed);
            else
                await PreviewAsync(parsed);
        }
        catch (SqlException ex) when (!saveToDatabase)
        {
            parsed.Warnings.Add($"未能连接数据库，仅完成 Excel 解析: {ex.Message}");
            ApplyOfflinePreview(parsed);
        }

        return parsed;
    }

    private async Task PreviewAsync(CustomerQuoteImportResult parsed)
    {
        using var db = new SqlConnection(_conn);
        var customers = (await db.QueryAsync<Customer>("SELECT Id, CustomerCode, CustomerName FROM Customers"))
            .ToList();
        ResolveCustomers(parsed, customers);
        ApplyExpectedPrices(parsed);
    }

    private async Task SaveAsync(CustomerQuoteImportResult parsed)
    {
        using var db = new SqlConnection(_conn);
        await db.OpenAsync();
        using var tx = (SqlTransaction)await db.BeginTransactionAsync();

        try
        {
            var customers = (await db.QueryAsync<Customer>(
                "SELECT Id, CustomerCode, CustomerName FROM Customers", transaction: tx)).ToList();
            ResolveCustomers(parsed, customers);

            var errorCount = CountErrors(parsed);
            if (errorCount > 0)
                throw new InvalidOperationException($"有 {errorCount} 行校验失败，已全部回滚。");

            var rulesInserted = 0;
            var lanes = 0;

            if (parsed.WideRows.Count > 0)
            {
                foreach (var row in parsed.WideRows)
                {
                    if (row.CustomerId == null)
                        continue;

                    await ReplaceLaneRulesAsync(db, tx, row.CustomerId.Value, row.Province, row.ExpressType);
                    var rules = CustomerQuoteRuleMapper.ToRules(row, row.CustomerId.Value);
                    rulesInserted += await InsertRulesAsync(db, tx, rules);
                    lanes++;
                }
            }
            else
            {
                foreach (var group in parsed.LongRows
                    .Where(r => string.IsNullOrEmpty(r.ErrorMessage) && r.CustomerId != null)
                    .GroupBy(r => new
                    {
                        r.CustomerId,
                        Province = r.Province.Trim(),
                        Express = r.ExpressType?.Trim() ?? ""
                    }))
                {
                    var sample = group.First();
                    await ReplaceLaneRulesAsync(db, tx, sample.CustomerId!.Value, sample.Province, sample.ExpressType);

                    foreach (var row in group)
                    {
                        var rule = CustomerQuoteRuleMapper.FromLongRow(row, row.CustomerId!.Value);
                        if (rule == null)
                        {
                            row.ErrorMessage = $"无法识别公斤段「{row.WeightBracket}」";
                            continue;
                        }

                        await InsertRuleAsync(db, tx, rule);
                        rulesInserted++;
                    }

                    lanes++;
                }

                errorCount = CountErrors(parsed);
                if (errorCount > 0)
                    throw new InvalidOperationException($"有 {errorCount} 行校验失败，已全部回滚。");
            }

            await tx.CommitAsync();
            parsed.SavedToDatabase = true;
            parsed.RulesUpserted = rulesInserted;
            parsed.LanesUpserted = lanes;
            ApplyExpectedPrices(parsed);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static void ApplyOfflinePreview(CustomerQuoteImportResult parsed)
    {
        foreach (var row in parsed.WideRows)
        {
            if (string.IsNullOrWhiteSpace(row.CustomerCode))
                row.ErrorMessage = "客户编号不能为空";
            else if (string.IsNullOrWhiteSpace(row.Province))
                row.ErrorMessage = "省份不能为空";
            else
                row.ImportNote = "预览（未连库）";
        }

        foreach (var row in parsed.LongRows)
        {
            if (string.IsNullOrWhiteSpace(row.CustomerCode))
                row.ErrorMessage = "客户编号不能为空";
            else if (string.IsNullOrWhiteSpace(row.Province))
                row.ErrorMessage = "省份不能为空";
            else if (WeightBracketParser.Parse(row.WeightBracket) == null)
                row.ErrorMessage = $"无法识别公斤段「{row.WeightBracket}」";
        }
    }

    private static void ResolveCustomers(CustomerQuoteImportResult parsed, List<Customer> customers)
    {
        var byCode = customers.ToDictionary(c => c.CustomerCode, StringComparer.OrdinalIgnoreCase);
        var byName = customers.GroupBy(c => c.CustomerName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var row in parsed.WideRows)
        {
            ValidateWideRow(row);
            if (!string.IsNullOrEmpty(row.ErrorMessage))
                continue;

            if (TryResolveCustomer(row.CustomerCode, row.CustomerName, byCode, byName, out var customer))
                row.CustomerId = customer!.Id;
            else
                row.ErrorMessage = $"客户「{row.CustomerCode}」不存在，请先在客户管理中添加";
        }

        foreach (var row in parsed.LongRows)
        {
            ValidateLongRow(row);
            if (!string.IsNullOrEmpty(row.ErrorMessage))
                continue;

            if (TryResolveCustomer(row.CustomerCode, row.CustomerName, byCode, byName, out var customer))
                row.CustomerId = customer!.Id;
            else
                row.ErrorMessage = $"客户「{row.CustomerCode}」/「{row.CustomerName ?? ""}」不存在，请先在客户管理中添加（师傅文件编号 93 可建 CustomerCode=93）";
        }
    }

    private static bool TryResolveCustomer(
        string customerCode,
        string? customerName,
        Dictionary<string, Customer> byCode,
        Dictionary<string, Customer> byName,
        out Customer? customer)
    {
        customer = null;
        var code = customerCode.Trim();
        if (byCode.TryGetValue(code, out customer))
            return true;

        if (!string.IsNullOrWhiteSpace(customerName)
            && byName.TryGetValue(customerName.Trim(), out customer))
            return true;

        return false;
    }

    private static void ValidateWideRow(CustomerQuoteTableRow row)
    {
        if (string.IsNullOrWhiteSpace(row.CustomerCode))
            row.ErrorMessage = "客户编号不能为空";
        else if (string.IsNullOrWhiteSpace(row.Province))
            row.ErrorMessage = "省份不能为空";
    }

    private static void ValidateLongRow(CustomerQuoteLongRow row)
    {
        if (string.IsNullOrWhiteSpace(row.CustomerCode))
            row.ErrorMessage = "客户编号不能为空";
        else if (string.IsNullOrWhiteSpace(row.Province))
            row.ErrorMessage = "省份不能为空";
        else if (WeightBracketParser.Parse(row.WeightBracket) == null)
            row.ErrorMessage = $"无法识别公斤段「{row.WeightBracket}」";
    }

    private static int CountErrors(CustomerQuoteImportResult parsed) =>
        parsed.WideRows.Count(r => !string.IsNullOrEmpty(r.ErrorMessage))
        + parsed.LongRows.Count(r => !string.IsNullOrEmpty(r.ErrorMessage));

    private static void ApplyExpectedPrices(CustomerQuoteImportResult parsed)
    {
        foreach (var row in parsed.WideRows)
        {
            if (!string.IsNullOrEmpty(row.ErrorMessage))
                continue;

            var calcRow = ToCalcRow(row);
            row.ExpectedPrice1Kg = PriceCalculator.Calculate(calcRow, 1m);
            row.ExpectedPrice3Kg = PriceCalculator.Calculate(calcRow, 3m);
        }
    }

    private static PriceTableRow ToCalcRow(CustomerQuoteTableRow row) => new()
    {
        Price_0_0_3 = row.Price_0_0_3,
        Price_0_3_0_5 = row.Price_0_3_0_5,
        Price_0_5_1 = row.Price_0_5_1,
        Price_1_2 = row.Price_1_2,
        Price_2_3 = row.Price_2_3,
        Price_3_4 = row.Price_3_4,
        Price_4_5 = row.Price_4_5,
        BaseFee = row.BaseFee,
        AdditionalUnitPrice = row.AdditionalUnitPrice
    };

    private static async Task ReplaceLaneRulesAsync(
        SqlConnection db,
        SqlTransaction tx,
        long customerId,
        string province,
        string? expressType)
    {
        await db.ExecuteAsync(@"
            DELETE FROM CustomerQuoteRules
            WHERE CustomerId = @CustomerId AND Province = @Province
              AND ((ExpressType IS NULL AND @ExpressType IS NULL)
                   OR ExpressType = @ExpressType)",
            new
            {
                CustomerId = customerId,
                Province = province.Trim(),
                ExpressType = string.IsNullOrWhiteSpace(expressType) ? null : expressType.Trim()
            }, tx);
    }

    private static async Task<int> InsertRulesAsync(SqlConnection db, SqlTransaction tx, List<CustomerQuoteRule> rules)
    {
        var count = 0;
        foreach (var rule in rules)
        {
            await InsertRuleAsync(db, tx, rule);
            count++;
        }

        return count;
    }

    private static Task InsertRuleAsync(SqlConnection db, SqlTransaction tx, CustomerQuoteRule rule) =>
        db.ExecuteAsync(@"
            INSERT INTO CustomerQuoteRules (
                CustomerId, Province, ExpressType, BillingType, MinWeight, MaxWeight,
                UnitPrice, BaseFee, EffectiveDate, ExpiryDate, Status, Remark)
            VALUES (
                @CustomerId, @Province, @ExpressType, @BillingType, @MinWeight, @MaxWeight,
                @UnitPrice, @BaseFee, @EffectiveDate, @ExpiryDate, @Status, @Remark)",
            rule, tx);
}
