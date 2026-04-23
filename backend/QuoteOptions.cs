namespace TickerScout.Backend;

public sealed class QuoteOptions
{
    public string[] Symbols { get; init; } = ["AAPL", "MSFT", "NVDA", "GOOGL", "AMZN"];

    public int UpdateIntervalMs { get; init; } = 750;
}
