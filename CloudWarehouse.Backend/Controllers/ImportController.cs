using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using CloudWarehouse.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImportController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".xlsx", ".xlsm"];
    private readonly PriceRuleImportService _importService;

    public ImportController(PriceRuleImportService importService)
    {
        _importService = importService;
    }

    /// <summary>下载云仓标准价格表模板（第1行表头，简单易填）。</summary>
    [HttpGet("price-table/template")]
    public ActionResult DownloadStandardTemplate()
    {
        var bytes = ExcelHelper.CreateStandardPriceTableTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "云仓价格表标准模板.xlsx");
    }

    /// <summary>预览解析结果（校验站点/目的地，不写库）。</summary>
    [HttpPost("price-table/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<PriceTableImportResult>>> PreviewPriceTable(IFormFile file)
    {
        return await ProcessUpload(file, saveToDatabase: false);
    }

    /// <summary>导入并写入数据库：校验站点/目的地，存在则替换该路线全部规则，任一行失败则整批回滚。</summary>
    [HttpPost("price-table")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<PriceTableImportResult>>> ImportPriceTable(IFormFile file)
    {
        return await ProcessUpload(file, saveToDatabase: true);
    }

    [HttpPost("price-table/export")]
    public ActionResult ExportPriceTableResult([FromBody] List<PriceTableRow> rows)
    {
        if (rows == null || rows.Count == 0)
            return BadRequest("没有可导出的数据");

        var bytes = ExcelHelper.ExportPriceTableResult(rows);
        var fileName = $"价格表导入结果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private async Task<ActionResult<ApiResponse<PriceTableImportResult>>> ProcessUpload(
        IFormFile? file, bool saveToDatabase)
    {
        if (file == null || file.Length == 0)
            return Ok(ApiResponse<PriceTableImportResult>.Fail("请选择要上传的 Excel 文件"));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Ok(ApiResponse<PriceTableImportResult>.Fail("仅支持 .xlsx / .xlsm 格式"));

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _importService.ProcessImportAsync(stream, saveToDatabase);
            return Ok(ApiResponse<PriceTableImportResult>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<PriceTableImportResult>.Fail(ex.Message));
        }
        catch (SqlException ex)
        {
            return Ok(ApiResponse<PriceTableImportResult>.Fail(
                $"数据库错误: {ex.Message}。请先执行 database/schema.sql 建库并配置连接字符串。"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<PriceTableImportResult>.Fail($"处理失败: {ex.Message}"));
        }
    }
}
