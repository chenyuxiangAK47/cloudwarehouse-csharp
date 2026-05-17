using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SiteController : ControllerBase
{
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
}