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

    public Quote? ApplyClientEdit(QuoteEdit edit)
    {
        if (string.IsNullOrWhiteSpace(edit.Symbol))
        {
            return null;
        }

        return _quotes.AddOrUpdate(
            edit.Symbol,
            symbol =>
            {
                var bid = Math.Max(0, edit.Bid ?? 0);
                var ask = Math.Max(bid, edit.Ask ?? bid + 0.01);

                return new Quote
                {
                    Symbol = symbol,
                    Bid = bid,
                    Ask = ask,
                    Last = Math.Round((bid + ask) / 2.0, 2),
                    Open = bid,
                    Close = ask,
                    BidSize = Math.Max(0, edit.BidSize ?? 0),
                    AskSize = Math.Max(0, edit.AskSize ?? 0),
                    Timestamp = DateTimeOffset.UtcNow
                };
            },
            (_, current) =>
            {
                if (edit.Bid is { } bid)
                {
                    current.Bid = Math.Max(0, bid);
                }

                if (edit.Ask is { } ask)
                {
                    current.Ask = Math.Max(current.Bid, ask);
                }

                if (edit.BidSize is { } bidSize)
                {
                    current.BidSize = Math.Max(0, bidSize);
                }

                if (edit.AskSize is { } askSize)
                {
                    current.AskSize = Math.Max(0, askSize);
                }

                current.Last = Math.Round((current.Bid + current.Ask) / 2.0, 2);
                current.Timestamp = DateTimeOffset.UtcNow;
                return current;
            });
    }

    public Quote[] GetSnapshot() =>
        _quotes.Values
            .OrderBy(q => q.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
