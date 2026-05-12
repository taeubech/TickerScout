using TickerScout.Backend.Services;

namespace TickerScout.Backend.Models;

public abstract class QuoteFilter
{
    public abstract bool Pass(Quote quote);
}


public sealed class InstrumentTypeFilter(IEnumerable<InstrumentType> instrumentTypes) : QuoteFilter
{
    public override bool Pass(Quote quote)
    {
        return instrumentTypes.Contains(quote.InstrumentType);
    }
}


public sealed class SymbolFilter(IEnumerable<string> symbols) : QuoteFilter
{
    public override bool Pass(Quote quote)
    {
        return symbols.Contains(quote.Symbol, StringComparer.OrdinalIgnoreCase);
    }
}


public sealed class CurrencyFilter : QuoteFilter
{
    private static readonly Lock InstrumentsCacheLock = new();
    private static Dictionary<string, Instrument>? _cachedInstrumentsBySymbol;

    private readonly Dictionary<string, Instrument> _instrumentsBySymbol;
    private readonly HashSet<string> _currencies;

    public CurrencyFilter(IEnumerable<string> currencies, IStaticDataService staticDataService)
    {
        _currencies = new HashSet<string>(
            currencies
                .Where(currency => !string.IsNullOrWhiteSpace(currency))
                .Select(currency => currency.Trim()),
            StringComparer.OrdinalIgnoreCase);

        if (_currencies.Count == 0)
        {
            throw new ArgumentException("At least one currency must be provided.", nameof(currencies));
        }

        lock (InstrumentsCacheLock)
        {
            _cachedInstrumentsBySymbol ??= staticDataService.GetAllInstruments()
                .Where(instrument => !string.IsNullOrWhiteSpace(instrument.Symbol))
                .GroupBy(instrument => instrument.Symbol.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToDictionary(instrument => instrument.Symbol.Trim(), StringComparer.OrdinalIgnoreCase);

            _instrumentsBySymbol = _cachedInstrumentsBySymbol;
        }
    }

    public override bool Pass(Quote quote)
    {
        if (string.IsNullOrWhiteSpace(quote.Symbol))
        {
            return false;
        }

        if (!_instrumentsBySymbol.TryGetValue(quote.Symbol.Trim(), out var instrument))
        {
            return false;
        }

        return _currencies.Contains(instrument.Currency);
    }
}


public sealed class LastGreaterThanFilter(double threshold) : QuoteFilter
{
    public override bool Pass(Quote quote)
    {
        return quote.Last > threshold;
    }
}


public sealed class NotFilter(QuoteFilter innerFilter) : QuoteFilter
{
    public override bool Pass(Quote quote)
    {
        return !innerFilter.Pass(quote);
    }
}


public sealed class  AndFilter(QuoteFilter filter1, QuoteFilter filter2) : QuoteFilter
{
    public override bool Pass(Quote quote)
    {
        return filter1.Pass(quote) && filter2.Pass(quote);
    }
}


public sealed class OrFilter(QuoteFilter filter1, QuoteFilter filter2) : QuoteFilter
{
    public override bool Pass(Quote quote)
    {
        return filter1.Pass(quote) || filter2.Pass(quote);
    }
}
