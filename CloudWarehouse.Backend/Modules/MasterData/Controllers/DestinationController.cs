using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DestinationController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".xlsx", ".xlsm"];
    private readonly string _conn;

    public DestinationController(IConfiguration config)
    {
        _conn = config.GetConnectionString("DefaultConnection")!;
    }

    // ============================================
    // 【C】Create - 创建目的地
    // POST: api/Destination
    // ============================================
    [HttpPost]
    public async Task<ActionResult<ApiResponse>> Create([FromBody] Destination destination)
    {
        try
        {
            destination.CreateTime = DateTime.Now;
            using var db = new SqlConnection(_conn);
            await db.ExecuteAsync(@"
                INSERT INTO Destinations (DestCode, Province, City, Area, CreateTime)
                VALUES (@DestCode, @Province, @City, @Area, @CreateTime)", destination);
            return Ok(ApiResponse.Ok("添加成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Fail($"添加失败: {ex.Message}"));
        }
    }

    // ============================================
    // 【R】Read - 获取所有目的地
    // GET: api/Destination
    // ============================================
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Destination>>>> GetAll()
    {
        try
        {
            using var db = new SqlConnection(_conn);
            var destinations = await db.QueryAsync<Destination>("SELECT * FROM Destinations ORDER BY CreateTime DESC");
            return Ok(ApiResponse<IEnumerable<Destination>>.Ok(destinations));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<IEnumerable<Destination>>.Fail($"获取失败: {ex.Message}"));
        }
    }

    // ============================================
    // 【R】Read - 获取单个目的地
    // GET: api/Destination/1
    // ============================================
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Destination>>> GetById(long id)
    {
        try
        {
            using var db = new SqlConnection(_conn);
            var destination = await db.QueryFirstOrDefaultAsync<Destination>("SELECT * FROM Destinations WHERE Id = @Id", new { Id = id });
            
            if (destination == null)
            {
                return Ok(ApiResponse<Destination>.Fail("目的地不存在"));
            }
            return Ok(ApiResponse<Destination>.Ok(destination));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<Destination>.Fail($"获取失败: {ex.Message}"));
        }
    }

    // ============================================
    // 【U】Update - 更新目的地
    // PUT: api/Destination/1
    // ============================================
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(long id, [FromBody] Destination destination)
    {
        try
        {
            destination.Id = id;
            using var db = new SqlConnection(_conn);
            await db.ExecuteAsync(@"
                UPDATE Destinations SET DestCode = @DestCode, Province = @Province, City = @City, Area = @Area
                WHERE Id = @Id", destination);
            return Ok(ApiResponse.Ok("更新成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Fail($"更新失败: {ex.Message}"));
        }
    }

    // ============================================
    // 【D】Delete - 删除目的地
    // DELETE: api/Destination/1
    // ============================================
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(long id)
    {
        try
        {
            using var db = new SqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM Destinations WHERE Id = @Id", new { Id = id });
            return Ok(ApiResponse.Ok("删除成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Fail($"删除失败: {ex.Message}"));
        }
    }

    [HttpGet("import/template")]
    public ActionResult DownloadImportTemplate()
    {
        var bytes = DestinationExcelHelper.CreateImportTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "目的地导入模板.xlsx");
    }

    [HttpPost("import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<DestinationImportResult>>> PreviewImport(IFormFile file) =>
        await ProcessImport(file, save: false);

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<DestinationImportResult>>> Import(IFormFile file) =>
        await ProcessImport(file, save: true);

    private async Task<ActionResult<ApiResponse<DestinationImportResult>>> ProcessImport(IFormFile file, bool save)
    {
        if (file == null || file.Length == 0)
            return Ok(ApiResponse<DestinationImportResult>.Fail("请上传 Excel 文件"));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Ok(ApiResponse<DestinationImportResult>.Fail("仅支持 .xlsx / .xlsm 格式"));

        try
        {
            await using var stream = file.OpenReadStream();
            var rows = DestinationExcelHelper.ReadDestinations(stream);
            ValidateImportRows(rows);

            var result = new DestinationImportResult { Rows = rows, TotalRows = rows.Count };
            if (!save)
                return Ok(ApiResponse<DestinationImportResult>.Ok(result));

            var errors = rows.Where(r => !string.IsNullOrEmpty(r.ErrorMessage)).ToList();
            if (errors.Count > 0)
                return Ok(ApiResponse<DestinationImportResult>.Fail(
                    $"导入失败，共 {errors.Count} 行有误。首条：第 {errors[0].RowNumber} 行 — {errors[0].ErrorMessage}"));

            using var db = new SqlConnection(_conn);
            await db.OpenAsync();
            using var tx = await db.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    var existingId = await db.QueryFirstOrDefaultAsync<long?>(
                        "SELECT Id FROM Destinations WHERE DestCode = @DestCode",
                        new { row.DestCode }, tx);

                    if (existingId.HasValue)
                    {
                        await db.ExecuteAsync(@"
                            UPDATE Destinations SET Province = @Province, City = @City, Area = @Area
                            WHERE Id = @Id",
                            new { row.Province, row.City, row.Area, Id = existingId.Value }, tx);
                        result.Updated++;
                    }
                    else
                    {
                        await db.ExecuteAsync(@"
                            INSERT INTO Destinations (DestCode, Province, City, Area, CreateTime)
                            VALUES (@DestCode, @Province, @City, @Area, SYSDATETIME())",
                            row, tx);
                        result.Inserted++;
                    }
                }

                await tx.CommitAsync();
                result.SavedToDatabase = true;
                return Ok(ApiResponse<DestinationImportResult>.Ok(result));
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<DestinationImportResult>.Fail(ex.Message));
        }
        catch (SqlException ex)
        {
            return Ok(ApiResponse<DestinationImportResult>.Fail($"数据库错误: {ex.Message}。"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<DestinationImportResult>.Fail($"导入失败: {ex.Message}"));
        }
    }

    private static void ValidateImportRows(List<DestinationImportRow> rows)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.DestCode))
                row.ErrorMessage = "目的地编码不能为空";
            else if (string.IsNullOrWhiteSpace(row.Province))
                row.ErrorMessage = "省份不能为空";
            else if (!seen.Add(row.DestCode))
                row.ErrorMessage = $"文件中目的地编码重复: {row.DestCode}";
        }
    }
}