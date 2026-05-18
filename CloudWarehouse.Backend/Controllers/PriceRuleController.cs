using CloudWarehouse.Backend.Models;
using CloudWarehouse.Backend.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PriceRuleController : ControllerBase
{
    private readonly string _conn;
    private readonly PriceRuleCalculateService _calculateService;

    public PriceRuleController(IConfiguration config, PriceRuleCalculateService calculateService)
    {
        _conn = config.GetConnectionString("DefaultConnection")!;
        _calculateService = calculateService;
    }

    /// <summary>查看已导入的价格规则（只读，规则由 Excel 导入维护）。</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PriceRule>>>> GetAll(
        [FromQuery] long? siteId, [FromQuery] long? destId)
    {
        try
        {
            using var db = new SqlConnection(_conn);
            var sql = "SELECT * FROM PriceRules WHERE 1=1";
            if (siteId.HasValue) sql += " AND SiteId = @SiteId";
            if (destId.HasValue) sql += " AND DestId = @DestId";
            sql += " ORDER BY SiteId, DestId, BillingType, MinWeight";

            var rules = await db.QueryAsync<PriceRule>(sql, new { SiteId = siteId, DestId = destId });
            return Ok(ApiResponse<IEnumerable<PriceRule>>.Ok(rules));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<IEnumerable<PriceRule>>.Fail($"获取失败: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PriceRule>>> GetById(long id)
    {
        try
        {
            using var db = new SqlConnection(_conn);
            var rule = await db.QueryFirstOrDefaultAsync<PriceRule>(
                "SELECT * FROM PriceRules WHERE Id = @Id", new { Id = id });
            if (rule == null)
                return Ok(ApiResponse<PriceRule>.Fail("规则不存在"));
            return Ok(ApiResponse<PriceRule>.Ok(rule));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<PriceRule>.Fail($"获取失败: {ex.Message}"));
        }
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<PriceCalculateResult>>> Calculate(
        [FromBody] PriceCalculateRequest request)
    {
        try
        {
            if (request.Weight <= 0)
                return Ok(ApiResponse<PriceCalculateResult>.Fail("请输入有效重量"));

            var result = await _calculateService.CalculateAsync(request);
            if (result == null)
                return Ok(ApiResponse<PriceCalculateResult>.Fail("未找到匹配的价格规则，请先导入价格表"));

            return Ok(ApiResponse<PriceCalculateResult>.Ok(result));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<PriceCalculateResult>.Fail($"计算失败: {ex.Message}"));
        }
    }
}
