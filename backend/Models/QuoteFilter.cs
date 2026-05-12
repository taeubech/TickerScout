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