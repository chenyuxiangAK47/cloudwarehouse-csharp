using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SiteController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".xlsx", ".xlsm"];
    private readonly string _conn;

    public SiteController(IConfiguration config)
    {
        _conn = config.GetConnectionString("DefaultConnection")!;
    }

    // ============================================
    // 【C】Create - 创建站点
    // POST: api/Site
    // ============================================
    [HttpPost]
    public async Task<ActionResult<ApiResponse>> Create([FromBody] Site site)
    {
        try
        {
            using var db = new SqlConnection(_conn);
            site.CreateTime = DateTime.Now;
                await db.ExecuteAsync(@"
                INSERT INTO Sites (SiteCode, SiteName, SiteType, ExpressCompany, ContactPerson, ContactPhone, Address, Status, CreateTime, Remark)
                VALUES (@SiteCode, @SiteName, @SiteType, @ExpressCompany, @ContactPerson, @ContactPhone, @Address, @Status, @CreateTime, @Remark)", site);
            return Ok(ApiResponse.Ok("添加成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Fail($"添加失败: {ex.Message}"));
        }
    }

    // ============================================
    // 【R】Read - 获取所有站点
    // GET: api/Site
    // ============================================
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Site>>>> GetAll()
    {
        try
        {
            using var db = new SqlConnection(_conn);
            var sites = await db.QueryAsync<Site>("SELECT * FROM Sites ORDER BY CreateTime DESC");
            return Ok(ApiResponse<IEnumerable<Site>>.Ok(sites));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<IEnumerable<Site>>.Fail($"获取失败: {ex.Message}"));
        }
    }

    // ============================================
    // 【R】Read - 获取单个站点
    // GET: api/Site/1
    // ============================================
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Site>>> GetById(long id)
    {
        try
        {
            using var db = new SqlConnection(_conn);
            var site = await db.QueryFirstOrDefaultAsync<Site>("SELECT * FROM Sites WHERE Id = @Id", new { Id = id });
            
            if (site == null)
            {
                return Ok(ApiResponse<Site>.Fail("站点不存在"));
            }
            return Ok(ApiResponse<Site>.Ok(site));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<Site>.Fail($"获取失败: {ex.Message}"));
        }
    }

    // ============================================
    // 【U】Update - 更新站点
    // PUT: api/Site/1
    // ============================================
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(long id, [FromBody] Site site)
    {
        try
        {
            site.Id = id;
            using var db = new SqlConnection(_conn);
            await db.ExecuteAsync(@"
                UPDATE Sites SET SiteCode = @SiteCode, SiteName = @SiteName, SiteType = @SiteType, 
                               ExpressCompany = @ExpressCompany, ContactPerson = @ContactPerson, 
                               ContactPhone = @ContactPhone, Address = @Address, Status = @Status, Remark = @Remark
                WHERE Id = @Id", site);
            return Ok(ApiResponse.Ok("更新成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Fail($"更新失败: {ex.Message}"));
        }
    }

    // ============================================
    // 【D】Delete - 删除站点
    // DELETE: api/Site/1
    // ============================================
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(long id)
    {
        try
        {
            using var db = new SqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM Sites WHERE Id = @Id", new { Id = id });
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
        var bytes = SiteExcelHelper.CreateImportTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "站点导入模板.xlsx");
    }

    [HttpPost("import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<SiteImportResult>>> PreviewImport(IFormFile file) =>
        await ProcessImport(file, save: false);

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<SiteImportResult>>> Import(IFormFile file) =>
        await ProcessImport(file, save: true);

    private async Task<ActionResult<ApiResponse<SiteImportResult>>> ProcessImport(IFormFile file, bool save)
    {
        if (file == null || file.Length == 0)
            return Ok(ApiResponse<SiteImportResult>.Fail("请上传 Excel 文件"));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Ok(ApiResponse<SiteImportResult>.Fail("仅支持 .xlsx / .xlsm 格式"));

        try
        {
            await using var stream = file.OpenReadStream();
            var rows = SiteExcelHelper.ReadSites(stream);
            ValidateImportRows(rows);

            var result = new SiteImportResult { Rows = rows, TotalRows = rows.Count };
            if (!save)
                return Ok(ApiResponse<SiteImportResult>.Ok(result));

            var errors = rows.Where(r => !string.IsNullOrEmpty(r.ErrorMessage)).ToList();
            if (errors.Count > 0)
                return Ok(ApiResponse<SiteImportResult>.Fail(
                    $"导入失败，共 {errors.Count} 行有误。首条：第 {errors[0].RowNumber} 行 — {errors[0].ErrorMessage}"));

            using var db = new SqlConnection(_conn);
            await db.OpenAsync();
            using var tx = await db.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    var existingId = await db.QueryFirstOrDefaultAsync<long?>(
                        "SELECT Id FROM Sites WHERE SiteCode = @SiteCode",
                        new { row.SiteCode }, tx);

                    if (existingId.HasValue)
                    {
                        await db.ExecuteAsync(@"
                            UPDATE Sites SET SiteName = @SiteName, SiteType = @SiteType,
                                ExpressCompany = @ExpressCompany, ContactPerson = @ContactPerson,
                                ContactPhone = @ContactPhone, Address = @Address, Status = @Status, Remark = @Remark
                            WHERE Id = @Id",
                            new
                            {
                                row.SiteName, row.SiteType, row.ExpressCompany, row.ContactPerson,
                                row.ContactPhone, row.Address, row.Status, row.Remark, Id = existingId.Value
                            }, tx);
                        result.Updated++;
                    }
                    else
                    {
                        await db.ExecuteAsync(@"
                            INSERT INTO Sites (SiteCode, SiteName, SiteType, ExpressCompany, ContactPerson,
                                ContactPhone, Address, Status, CreateTime, Remark)
                            VALUES (@SiteCode, @SiteName, @SiteType, @ExpressCompany, @ContactPerson,
                                @ContactPhone, @Address, @Status, SYSDATETIME(), @Remark)",
                            row, tx);
                        result.Inserted++;
                    }
                }

                await tx.CommitAsync();
                result.SavedToDatabase = true;
                return Ok(ApiResponse<SiteImportResult>.Ok(result));
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<SiteImportResult>.Fail(ex.Message));
        }
        catch (SqlException ex)
        {
            return Ok(ApiResponse<SiteImportResult>.Fail($"数据库错误: {ex.Message}。"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<SiteImportResult>.Fail($"导入失败: {ex.Message}"));
        }
    }

    private static void ValidateImportRows(List<SiteImportRow> rows)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.SiteCode))
                row.ErrorMessage = "站点编号不能为空";
            else if (string.IsNullOrWhiteSpace(row.SiteName))
                row.ErrorMessage = "站点名称不能为空";
            else if (!seen.Add(row.SiteCode))
                row.ErrorMessage = $"文件中站点编号重复: {row.SiteCode}";
        }
    }
}