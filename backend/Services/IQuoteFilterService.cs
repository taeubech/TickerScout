using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public interface IQuoteFilterService
{
    bool Pass(string connectionId, Quote quote);
    void AddFilters(string connectionId, IEnumerable<QuoteFilter> filters);
    void RemoveFilters(string connectionId);    
}
