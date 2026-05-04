using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public interface IQuoteFilterService
{
    bool Filter(string sessionId, IEnumerable<QuoteFilter> filters);
}
