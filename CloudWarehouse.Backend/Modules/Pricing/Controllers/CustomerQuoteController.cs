using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;
using CloudWarehouse.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CustomerQuoteController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".xlsx", ".xlsm"];
    private readonly CustomerQuoteImportService _importService;

    public CustomerQuoteController(CustomerQuoteImportService importService)
    {
        _importService = importService;
    }

    [HttpGet("template")]
    public ActionResult DownloadTemplate()
    {
        var bytes = CustomerQuoteExcelHelper.CreateStandardTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "云仓客户报价标准模板.xlsx");
    }

    [HttpPost("preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<CustomerQuoteImportResult>>> Preview(IFormFile file)
    {
        return await ProcessUpload(file, saveToDatabase: false);
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<CustomerQuoteImportResult>>> Import(IFormFile file)
    {
        return await ProcessUpload(file, saveToDatabase: true);
    }

    [HttpPost("export")]
    public ActionResult ExportResult([FromBody] CustomerQuoteImportResult? data)
    {
        var rows = data?.WideRows ?? [];
        if (rows.Count == 0)
            return BadRequest("没有可导出的数据（宽表格式）");

        var bytes = CustomerQuoteExcelHelper.ExportWideResult(rows);
        var fileName = $"客户报价导入结果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private async Task<ActionResult<ApiResponse<CustomerQuoteImportResult>>> ProcessUpload(
        IFormFile? file, bool saveToDatabase)
    {
        if (file == null || file.Length == 0)
            return Ok(ApiResponse<CustomerQuoteImportResult>.Fail("请选择要上传的 Excel 文件"));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Ok(ApiResponse<CustomerQuoteImportResult>.Fail("仅支持 .xlsx / .xlsm 格式"));

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _importService.ProcessImportAsync(stream, saveToDatabase);
            return Ok(ApiResponse<CustomerQuoteImportResult>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<CustomerQuoteImportResult>.Fail(ex.Message));
        }
        catch (SqlException ex)
        {
            var hint = ex.Message.Contains("CustomerQuoteRules", StringComparison.OrdinalIgnoreCase)
                ? " 请执行 database/customer-quote-schema.sql。"
                : " 请确认已建库且连接字符串正确。";
            return Ok(ApiResponse<CustomerQuoteImportResult>.Fail($"数据库错误: {ex.Message}。{hint}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<CustomerQuoteImportResult>.Fail($"处理失败: {ex.Message}"));
        }
    }
}
