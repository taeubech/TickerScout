using Microsoft.AspNetCore.Mvc;
using TickerScout.Backend.Models;
using TickerScout.Backend.Services;

namespace TickerScout.Backend.Controllers;

[ApiController]
[Route("api/filter")]
public sealed class FilterController(
    IQuoteFilterService quoteFilterService,
    IStaticDataService staticDataService,
    ILogger<FilterController> logger) : ControllerBase
{
    private readonly IQuoteFilterService _quoteFilterService = quoteFilterService;
    private readonly IStaticDataService _staticDataService = staticDataService;
    private readonly ILogger<FilterController> _logger = logger;

    /// <summary>
    /// Sets a filter on quotes for the given connection by instrument type.
    /// </summary>
    [HttpPost("instrument")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult FilterInstrument([FromQuery] string sessionId, [FromQuery] InstrumentType? instrumentType)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest($"{nameof(sessionId)} must not be empty.");
        }

        if (instrumentType == null)
        {
            _quoteFilterService.SetFilters(sessionId, null);
            return NoContent();
        }

        if (!Enum.IsDefined(instrumentType!.Value))
        {
            return BadRequest("instrumentType is not a valid value.");
        }

        try
        {
            var filter = new InstrumentTypeFilter([instrumentType.Value]);

            _quoteFilterService.SetFilters(sessionId, filter);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting instrument filter for connection.");
            return Problem(detail: "An error occurred while setting the instrument filter.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Sets a filter on quotes for the given connection by currency.
    /// </summary>
    [HttpPost("currency")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult FilterCurrency([FromQuery] string sessionId, [FromQuery] string[]? currencies)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest($"{nameof(sessionId)} must not be empty.");
        }

        string[] normalizedCurrencies = (currencies ?? [])
            .Where(currency => !string.IsNullOrWhiteSpace(currency))
            .Select(currency => currency.Trim())
            .ToArray();

        if (normalizedCurrencies.Length == 0)
        {
            _quoteFilterService.SetFilters(sessionId, null);
            return NoContent();
        }

        try
        {
            var filter = new CurrencyFilter(normalizedCurrencies, _staticDataService.GetAllInstruments());

            _quoteFilterService.SetFilters(sessionId, filter);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting currency filter for session.");
            return Problem(detail: "An error occurred while setting the currency filter.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
