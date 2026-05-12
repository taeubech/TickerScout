using System.Collections.Concurrent;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class QuoteFilterService() : IQuoteFilterService
{
    private readonly ConcurrentDictionary<string, QuoteFilter> _filtersPerSession = new();

    public event Action<string>? FiltersChanged;


    public bool Pass(string sessionId, Quote quote)
    {
        if (!_filtersPerSession.TryGetValue(sessionId, out var filter))
            return true; // pass if no filters are configured

        return filter.Pass(quote);
    }

    public void SetFilters(string sessionId, QuoteFilter? filter)
    {
        if (filter != null)
        {
            _filtersPerSession.AddOrUpdate(
                sessionId,
                (connId) => filter,
                (connId, existing) => filter);
        }
        else 
        {             
            _filtersPerSession.TryRemove(sessionId, out _);
        }

        FiltersChanged?.Invoke(sessionId);
    }
}
