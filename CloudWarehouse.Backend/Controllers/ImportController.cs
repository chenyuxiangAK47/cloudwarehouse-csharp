using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImportController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".xlsx", ".xlsm"];

    /// <summary>
    /// 上传价格表 Excel，按第 3 行表头解析，返回含预期价格的结果（不写数据库）。
    /// </summary>
    [HttpPost("price-table")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<PriceTableImportResult>>> ImportPriceTable(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Ok(ApiResponse<PriceTableImportResult>.Fail("请选择要上传的 Excel 文件"));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Ok(ApiResponse<PriceTableImportResult>.Fail("仅支持 .xlsx / .xlsm 格式"));

        try
        {
            await using var stream = file.OpenReadStream();
            var result = ExcelHelper.ReadPriceTable(stream);
            return Ok(ApiResponse<PriceTableImportResult>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<PriceTableImportResult>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<PriceTableImportResult>.Fail($"解析失败: {ex.Message}"));
        }
    }

    /// <summary>将导入结果（含预期价格列）导出为 Excel</summary>
    [HttpPost("price-table/export")]
    public ActionResult ExportPriceTableResult([FromBody] List<PriceTableRow> rows)
    {
        if (rows == null || rows.Count == 0)
            return BadRequest("没有可导出的数据");

        var bytes = ExcelHelper.ExportPriceTableResult(rows);
        var fileName = $"价格表导入结果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
