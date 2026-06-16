using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using TickerScout.Backend.Models;
using TickerScout.Backend.Services;

namespace TickerScout.Backend.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class AIController(IAiService aiService, ILogger<AIController> logger) : ControllerBase
{
    private readonly IAiService _aiService = aiService;
    private readonly ILogger<AIController> _logger = logger;

    /// <summary>
    /// Sends a user prompt to the AI agent and returns its reply.
    /// </summary>
    [HttpPost("prompt")]
    [ProducesResponseType<AiPromptResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Prompt(
        [FromBody] AiPromptRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _aiService.ProcessPromptAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (CredentialUnavailableException ex)
        {
            _logger.LogError(ex, "Azure credential unavailable for AI service.");
            return Problem(
                detail: "The AI service credentials are not configured. " +
                        "Set the Ai:Username and Ai:AccessToken configuration values " +
                        "(or the Ai__Username and Ai__AccessToken environment variables) for non-Azure deployments.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error processing AI prompt.");
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
