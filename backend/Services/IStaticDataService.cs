using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public interface IStaticDataService
{
    IEnumerable<Instrument> GetAllInstruments();
}
