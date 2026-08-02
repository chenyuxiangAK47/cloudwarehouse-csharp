using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using CloudWarehouse.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BillController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".xlsx", ".xlsm"];
    private readonly BillImportService _importService;

    public BillController(BillImportService importService)
    {
        _importService = importService;
    }

    [HttpGet("waybill/template")]
    public ActionResult DownloadWaybillTemplate()
    {
        var bytes = WaybillExcelHelper.CreateStandardTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "云仓运单标准模板.xlsx");
    }

    [HttpPost("waybill/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<WaybillImportResult>>> PreviewWaybills(IFormFile file)
    {
        return await ProcessUpload(file, saveToDatabase: false);
    }

    [HttpPost("waybill")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<WaybillImportResult>>> ImportWaybills(IFormFile file)
    {
        return await ProcessUpload(file, saveToDatabase: true);
    }

    [HttpPost("waybill/export")]
    public ActionResult ExportWaybillResult([FromBody] List<WaybillImportRow> rows)
    {
        if (rows == null || rows.Count == 0)
            return BadRequest("没有可导出的数据");

        var bytes = WaybillExcelHelper.ExportResult(rows);
        var fileName = $"运单结算结果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private async Task<ActionResult<ApiResponse<WaybillImportResult>>> ProcessUpload(
        IFormFile? file, bool saveToDatabase)
    {
        if (file == null || file.Length == 0)
            return Ok(ApiResponse<WaybillImportResult>.Fail("请选择要上传的 Excel 文件"));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Ok(ApiResponse<WaybillImportResult>.Fail("仅支持 .xlsx / .xlsm 格式"));

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _importService.ProcessImportAsync(stream, saveToDatabase);
            return Ok(ApiResponse<WaybillImportResult>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<WaybillImportResult>.Fail(ex.Message));
        }
        catch (SqlException ex)
        {
            var hint = ex.Message.Contains("BillLines", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
                ? " 请执行 database/billing-schema.sql。"
                : " 请确认已建库且连接字符串正确。";
            return Ok(ApiResponse<WaybillImportResult>.Fail($"数据库错误: {ex.Message}。{hint}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<WaybillImportResult>.Fail($"处理失败: {ex.Message}"));
        }
    }
}
