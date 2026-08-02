using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CustomerController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".xlsx", ".xlsm"];
    private readonly string _conn;

    public CustomerController(IConfiguration config)
    {
        _conn = config.GetConnectionString("DefaultConnection")!;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Customer>>>> GetAll()
    {
        try
        {
            using var db = new SqlConnection(_conn);
            var list = await db.QueryAsync<Customer>(
                "SELECT * FROM Customers ORDER BY CustomerCode");
            return Ok(ApiResponse<IEnumerable<Customer>>.Ok(list));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<IEnumerable<Customer>>.Fail($"获取失败: {ex.Message}"));
        }
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<Customer>>> GetById(long id)
    {
        try
        {
            using var db = new SqlConnection(_conn);
            var item = await db.QueryFirstOrDefaultAsync<Customer>(
                "SELECT * FROM Customers WHERE Id = @Id", new { Id = id });
            if (item == null)
                return Ok(ApiResponse<Customer>.Fail("客户不存在"));
            return Ok(ApiResponse<Customer>.Ok(item));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<Customer>.Fail($"获取失败: {ex.Message}"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse>> Create([FromBody] Customer customer)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customer.CustomerCode) ||
                string.IsNullOrWhiteSpace(customer.CustomerName))
                return Ok(ApiResponse.Fail("请填写客户编号和名称"));

            customer.CreateTime = DateTime.Now;
            using var db = new SqlConnection(_conn);
            await db.ExecuteAsync(@"
                INSERT INTO Customers (CustomerCode, CustomerName, Status, CreateTime, Remark)
                VALUES (@CustomerCode, @CustomerName, @Status, @CreateTime, @Remark)", customer);
            return Ok(ApiResponse.Ok("添加成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Fail($"添加失败: {ex.Message}"));
        }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse>> Update(long id, [FromBody] Customer customer)
    {
        try
        {
            customer.Id = id;
            using var db = new SqlConnection(_conn);
            await db.ExecuteAsync(@"
                UPDATE Customers
                SET CustomerCode = @CustomerCode, CustomerName = @CustomerName,
                    Status = @Status, Remark = @Remark
                WHERE Id = @Id", customer);
            return Ok(ApiResponse.Ok("更新成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Fail($"更新失败: {ex.Message}"));
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse>> Delete(long id)
    {
        try
        {
            using var db = new SqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM Customers WHERE Id = @Id", new { Id = id });
            return Ok(ApiResponse.Ok("删除成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Fail($"删除失败: {ex.Message}"));
        }
    }

    [HttpGet("import/template")]
    public ActionResult DownloadTemplate()
    {
        var bytes = CustomerExcelHelper.CreateImportTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "客户导入模板.xlsx");
    }

    [HttpPost("import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<CustomerImportResult>>> PreviewImport(IFormFile file)
    {
        return await ProcessImport(file, save: false);
    }

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<CustomerImportResult>>> Import(IFormFile file)
    {
        return await ProcessImport(file, save: true);
    }

    private async Task<ActionResult<ApiResponse<CustomerImportResult>>> ProcessImport(IFormFile file, bool save)
    {
        if (file == null || file.Length == 0)
            return Ok(ApiResponse<CustomerImportResult>.Fail("请上传 Excel 文件"));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Ok(ApiResponse<CustomerImportResult>.Fail("仅支持 .xlsx / .xlsm 格式"));

        try
        {
            await using var stream = file.OpenReadStream();
            var rows = CustomerExcelHelper.ReadCustomers(stream);
            ValidateRows(rows);

            var result = new CustomerImportResult
            {
                Rows = rows,
                TotalRows = rows.Count
            };

            if (!save)
                return Ok(ApiResponse<CustomerImportResult>.Ok(result));

            var errors = rows.Where(r => !string.IsNullOrEmpty(r.ErrorMessage)).ToList();
            if (errors.Count > 0)
                return Ok(ApiResponse<CustomerImportResult>.Fail(
                    $"导入失败，共 {errors.Count} 行有误。首条：第 {errors[0].RowNumber} 行 — {errors[0].ErrorMessage}"));

            using var db = new SqlConnection(_conn);
            await db.OpenAsync();
            using var tx = await db.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    var existingId = await db.QueryFirstOrDefaultAsync<long?>(
                        "SELECT Id FROM Customers WHERE CustomerCode = @CustomerCode",
                        new { row.CustomerCode }, tx);

                    if (existingId.HasValue)
                    {
                        await db.ExecuteAsync(@"
                            UPDATE Customers SET CustomerName = @CustomerName, Status = 1
                            WHERE Id = @Id",
                            new { row.CustomerName, Id = existingId.Value }, tx);
                        result.Updated++;
                    }
                    else
                    {
                        await db.ExecuteAsync(@"
                            INSERT INTO Customers (CustomerCode, CustomerName, Status, CreateTime)
                            VALUES (@CustomerCode, @CustomerName, 1, SYSDATETIME())",
                            row, tx);
                        result.Inserted++;
                    }
                }

                await tx.CommitAsync();
                result.SavedToDatabase = true;
                return Ok(ApiResponse<CustomerImportResult>.Ok(result));
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<CustomerImportResult>.Fail(ex.Message));
        }
        catch (SqlException ex)
        {
            var hint = ex.Message.Contains("Customers", StringComparison.OrdinalIgnoreCase)
                ? " 请先执行 database/customers-table.sql 创建 dbo.Customers 表。"
                : "";
            return Ok(ApiResponse<CustomerImportResult>.Fail($"数据库错误: {ex.Message}。{hint}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<CustomerImportResult>.Fail($"导入失败: {ex.Message}"));
        }
    }

    private static void ValidateRows(List<CustomerImportRow> rows)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.CustomerCode))
                row.ErrorMessage = "客户编号不能为空";
            else if (string.IsNullOrWhiteSpace(row.CustomerName))
                row.ErrorMessage = "客户名称不能为空";
            else if (!seen.Add(row.CustomerCode))
                row.ErrorMessage = $"文件中客户编号重复: {row.CustomerCode}";
        }
    }
}
