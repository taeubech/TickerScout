using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public interface IQuoteFilterService
{
    event Action<string>? FiltersChanged;
    bool Pass(string connectionId, Quote quote);
    void SetFilters(string sessionId, IEnumerable<QuoteFilter> filters);
}
