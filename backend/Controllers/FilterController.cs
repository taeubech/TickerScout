using Microsoft.AspNetCore.Mvc;
using TickerScout.Backend.Models;
using TickerScout.Backend.Services;

namespace TickerScout.Backend.Controllers;

[ApiController]
[Route("api/filter")]
public sealed class FilterController(IQuoteFilterService quoteFilterService, ILogger<FilterController> logger) : ControllerBase
{
    private readonly IQuoteFilterService _quoteFilterService = quoteFilterService;
    private readonly ILogger<FilterController> _logger = logger;

    /// <summary>
    /// Sets a filter on quotes for the given connection by instrument type.
    /// </summary>
    [HttpPost("instrument")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult FilterInstrument([FromQuery] string connectionId, [FromQuery] InstrumentType instrumentType)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return BadRequest("connectionId must not be empty.");
        }

        if (!Enum.IsDefined(instrumentType))
        {
            return BadRequest("instrumentType is not a valid value.");
        }

        try
        {
            var filter = new QuoteFilter
            {
                Field = QuoteField.InstrumentType,
                Operator = FilterOperator.Equals,
                Value = instrumentType.ToString()
            };

            _quoteFilterService.SetFilters(connectionId, [filter]);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting instrument filter for connection.");
            return Problem(detail: "An error occurred while setting the instrument filter.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
