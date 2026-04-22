namespace TickerScout.Backend.Models;

public sealed class QuoteEdit
{
    public required string Symbol { get; init; }

    public double? Bid { get; init; }

    public double? Ask { get; init; }

    public int? BidSize { get; init; }

    public int? AskSize { get; init; }
}
