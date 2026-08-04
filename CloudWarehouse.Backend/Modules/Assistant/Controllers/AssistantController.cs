using CloudWarehouse.Backend.Models;
using CloudWarehouse.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace CloudWarehouse.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssistantController : ControllerBase
{
    private readonly IQuoteAssistantService _assistant;

    public AssistantController(IQuoteAssistantService assistant)
    {
        _assistant = assistant;
    }

    /// <summary>List loaded knowledge sources (for UI / demo evidence).</summary>
    [HttpGet("knowledge")]
    public ActionResult<ApiResponse<object>> Knowledge()
    {
        return Ok(ApiResponse<object>.Ok(_assistant.ListKnowledge()));
    }

    /// <summary>Ask the freight/quote assistant (RAG-lite).</summary>
    [HttpPost("ask")]
    public async Task<ActionResult<ApiResponse<AssistantAskResponse>>> Ask(
        [FromBody] AssistantAskRequest request,
        CancellationToken ct)
    {
        var result = await _assistant.AskAsync(request ?? new AssistantAskRequest(), ct);
        return Ok(ApiResponse<AssistantAskResponse>.Ok(result));
    }
}
