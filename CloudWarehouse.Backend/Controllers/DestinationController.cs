using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DestinationController : ControllerBase
{
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
}