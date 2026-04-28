using Microsoft.AspNetCore.Mvc;
using TickerScout.Backend.Models;
using TickerScout.Backend.Services;

namespace TickerScout.Backend.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class AIController(IAiService aiService) : ControllerBase
{
    private readonly IAiService _aiService = aiService;

    /// <summary>
    /// Sends a user prompt to the AI agent and returns its reply.
    /// </summary>
    [HttpPost("prompt")]
    [ProducesResponseType<AiPromptResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Prompt(
        [FromBody] AiPromptRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _aiService.ProcessPromptAsync(request, cancellationToken);
        return Ok(response);
    }
}
