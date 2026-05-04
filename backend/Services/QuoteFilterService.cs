using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class QuoteFilterService(SessionStore sessionStore, QuoteStore quoteStore) : IQuoteFilterService
{
    private readonly SessionStore _sessionStore = sessionStore;
    private readonly QuoteStore _quoteStore = quoteStore;

    /// <summary>
    /// Returns <c>true</c> when the session identified by <paramref name="sessionId"/> is valid
    /// and at least one quote in the current snapshot satisfies every filter in
    /// <paramref name="filters"/>.
    /// </summary>
    public bool Filter(string sessionId, IEnumerable<QuoteFilter> filters)
    {
        if (string.IsNullOrWhiteSpace(sessionId) ||
            _sessionStore.GetBySessionId(sessionId) is null)
        {
            return false;
        }

        var filterList = filters?.ToList() ?? [];

        if (filterList.Count == 0)
        {
            return true;
        }

        return _quoteStore.GetSnapshot()
            .Any(quote => filterList.All(f => MatchesFilter(quote, f)));
    }

    private static bool MatchesFilter(Quote quote, QuoteFilter filter)
    {
        return filter.Field switch
        {
            QuoteField.Symbol => ApplyStringOperator(quote.Symbol, filter.Operator, filter.Value),
            QuoteField.InstrumentType => ApplyStringOperator(quote.InstrumentType.ToString(), filter.Operator, filter.Value),
            QuoteField.Bid => ApplyNumericOperator(quote.Bid, filter.Operator, filter.Value),
            QuoteField.Ask => ApplyNumericOperator(quote.Ask, filter.Operator, filter.Value),
            QuoteField.Last => ApplyNumericOperator(quote.Last, filter.Operator, filter.Value),
            QuoteField.Open => ApplyNumericOperator(quote.Open, filter.Operator, filter.Value),
            QuoteField.Close => ApplyNumericOperator(quote.Close, filter.Operator, filter.Value),
            QuoteField.BidSize => ApplyNumericOperator(quote.BidSize, filter.Operator, filter.Value),
            QuoteField.AskSize => ApplyNumericOperator(quote.AskSize, filter.Operator, filter.Value),
            _ => false
        };
    }

    private static bool ApplyStringOperator(string fieldValue, FilterOperator op, string filterValue)
    {
        return op switch
        {
            FilterOperator.Equals => string.Equals(fieldValue, filterValue, StringComparison.OrdinalIgnoreCase),
            FilterOperator.NotEquals => !string.Equals(fieldValue, filterValue, StringComparison.OrdinalIgnoreCase),
            FilterOperator.Contains => fieldValue.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool ApplyNumericOperator(double fieldValue, FilterOperator op, string filterValue)
    {
        if (!double.TryParse(filterValue, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var numericValue))
        {
            return false;
        }

        return op switch
        {
            FilterOperator.Equals => fieldValue == numericValue,
            FilterOperator.NotEquals => fieldValue != numericValue,
            FilterOperator.GreaterThan => fieldValue > numericValue,
            FilterOperator.GreaterThanOrEquals => fieldValue >= numericValue,
            FilterOperator.LessThan => fieldValue < numericValue,
            FilterOperator.LessThanOrEquals => fieldValue <= numericValue,
            _ => false
        };
    }
}
