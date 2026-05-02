using Microsoft.AspNetCore.Mvc;
using TickerScout.Backend.Models;
using TickerScout.Backend.Services;

namespace TickerScout.Backend.Controllers;

[ApiController]
[Route("api/staticdata")]
public sealed class StaticDataController(IStaticDataService staticDataService, ILogger<StaticDataController> logger) : ControllerBase
{
    private readonly IStaticDataService _staticDataService = staticDataService;
    private readonly ILogger<StaticDataController> _logger = logger;

    /// <summary>
    /// Returns all instruments from the static data store.
    /// </summary>
    [HttpGet("instruments")]
    [ProducesResponseType<IEnumerable<Instrument>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetAllInstruments()
    {
        try
        {
            IEnumerable<Instrument> instruments = _staticDataService.GetAllInstruments();
            return Ok(instruments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving instruments.");
            return Problem(detail: $"An error occurred while retrieving instruments. {ex.Message}", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
