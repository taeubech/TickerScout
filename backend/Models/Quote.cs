namespace TickerScout.Backend.Models;

public sealed class Quote
{
    public required string Symbol { get; init; }

    public double Bid { get; set; }

    public double Ask { get; set; }

    public double Last { get; set; }

    public double Open { get; set; }

    public double Close { get; set; }

    public int BidSize { get; set; }

    public int AskSize { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}
