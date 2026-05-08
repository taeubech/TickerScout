using System.Collections.Concurrent;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class QuoteFilterService() : IQuoteFilterService
{
    private readonly ConcurrentDictionary<string, IEnumerable<QuoteFilter>> _filtersPerSession = new();

    public event Action<string>? FiltersChanged;

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

    private static bool ApplyStringOperator(string? fieldValue, FilterOperator op, string filterValue)
    {
        if (fieldValue is null)
        {
            return op == FilterOperator.NotEquals;
        }

        if (filterValue is null)
        {
            return true;
        }

        return op switch
        {
            FilterOperator.Equals => string.Equals(fieldValue, filterValue, StringComparison.OrdinalIgnoreCase),
            FilterOperator.NotEquals => !string.Equals(fieldValue, filterValue, StringComparison.OrdinalIgnoreCase),
            FilterOperator.Contains => fieldValue.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private const double NumericEpsilon = 1e-9;

    private static bool ApplyNumericOperator(double fieldValue, FilterOperator op, string filterValue)
    {
        if (!double.TryParse(filterValue, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var numericValue))
        {
            return false;
        }

        return op switch
        {
            FilterOperator.Equals => Math.Abs(fieldValue - numericValue) <= NumericEpsilon,
            FilterOperator.NotEquals => Math.Abs(fieldValue - numericValue) > NumericEpsilon,
            FilterOperator.GreaterThan => fieldValue > numericValue,
            FilterOperator.GreaterThanOrEquals => fieldValue >= numericValue || Math.Abs(fieldValue - numericValue) <= NumericEpsilon,
            FilterOperator.LessThan => fieldValue < numericValue,
            FilterOperator.LessThanOrEquals => fieldValue <= numericValue || Math.Abs(fieldValue - numericValue) <= NumericEpsilon,
            _ => false
        };
    }

    public bool Pass(string sessionId, Quote quote)
    {
        if (!_filtersPerSession.TryGetValue(sessionId, out var filters))
            return true; // pass if no filters are configured

        return filters.Any(filter => MatchesFilter(quote, filter));
    }

    public void SetFilters(string sessionId, IEnumerable<QuoteFilter> filters)
    {
        if (filters.Any())
        {
            _filtersPerSession.AddOrUpdate(
                sessionId,
                (connId) => filters.ToList(),
                (connId, existing) => filters.ToList());
        }
        else 
        {             
            _filtersPerSession.TryRemove(sessionId, out _);
        }

        FiltersChanged?.Invoke(sessionId);
    }

    public void RemoveFilters(string sessionId)
    {
        _filtersPerSession.TryRemove(sessionId, out _);
    }
}
