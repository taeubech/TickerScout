using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public interface IQuoteFilterService
{
    bool Pass(string connectionId, Quote quote);
    void SetFilters(string connectionId, IEnumerable<QuoteFilter> filters);
    void RemoveFilters(string connectionId);    
}
