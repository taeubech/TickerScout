namespace TickerScout.Backend.Models;

public enum QuoteField
{
    Symbol,
    Bid,
    Ask,
    Last,
    Open,
    Close,
    BidSize,
    AskSize,
    InstrumentType
}

public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEquals,
    LessThan,
    LessThanOrEquals,
    Contains
}

public sealed class QuoteFilter
{
    public required QuoteField Field { get; init; }

    public required FilterOperator Operator { get; init; }

    public required string Value { get; init; }
}
