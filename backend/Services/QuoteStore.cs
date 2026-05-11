using System.Collections.Concurrent;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class QuoteStore
{
    private readonly ConcurrentDictionary<string, Quote> _quotes = new(StringComparer.OrdinalIgnoreCase);

    public Quote Upsert(Quote quote)
    {
        _quotes.AddOrUpdate(quote.Symbol, quote, (_, _) => quote);
        return quote;
    }

    public Quote[] GetSnapshot() =>
        _quotes.Values
            .OrderBy(q => q.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
