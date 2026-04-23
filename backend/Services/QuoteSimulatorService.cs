using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class QuoteSimulatorService(
    IOptions<QuoteOptions> quoteOptions,
    QuoteStore quoteStore,
    IHubContext<QuoteHub> hubContext,
    ILogger<QuoteSimulatorService> logger) : BackgroundService
{
    private readonly QuoteOptions _options = quoteOptions.Value;
    private readonly QuoteStore _quoteStore = quoteStore;
    private readonly IHubContext<QuoteHub> _hubContext = hubContext;
    private readonly ILogger<QuoteSimulatorService> _logger = logger;
    private readonly Random _random = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbols = _options.Symbols
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (symbols.Length == 0)
        {
            _logger.LogWarning("Quote simulator has no symbols configured.");
            return;
        }

        var delay = TimeSpan.FromMilliseconds(Math.Max(100, _options.UpdateIntervalMs));
        var quotes = symbols.Select(SeedQuote).ToDictionary(q => q.Symbol, StringComparer.OrdinalIgnoreCase);

        foreach (var quote in quotes.Values)
        {
            _quoteStore.Upsert(quote);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var symbol in symbols)
            {
                var next = NextQuote(quotes[symbol]);
                quotes[symbol] = next;
                _quoteStore.Upsert(next);
                await _hubContext.Clients.All.SendAsync("ReceiveQuote", next, stoppingToken);
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    private Quote SeedQuote(string symbol)
    {
        var open = Math.Round(50 + _random.NextDouble() * 250, 2);
        var spread = Math.Round(0.01 + _random.NextDouble() * 0.2, 2);
        var bid = Math.Round(open - (spread / 2), 2);
        var ask = Math.Round(open + (spread / 2), 2);

        return new Quote
        {
            Symbol = symbol,
            Open = open,
            Close = open,
            Last = open,
            Bid = bid,
            Ask = ask,
            BidSize = _random.Next(25, 5000),
            AskSize = _random.Next(25, 5000),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private Quote NextQuote(Quote current)
    {
        var drift = (_random.NextDouble() - 0.5) * 0.8;
        var nextLast = Math.Round(Math.Max(0.01, current.Last + drift), 2);
        var spread = Math.Round(0.01 + _random.NextDouble() * 0.15, 2);
        var nextBid = Math.Round(Math.Max(0.01, nextLast - (spread / 2)), 2);
        var nextAsk = Math.Round(Math.Max(nextBid, nextLast + (spread / 2)), 2);

        return new Quote
        {
            Symbol = current.Symbol,
            Open = current.Open,
            Close = nextLast,
            Last = nextLast,
            Bid = nextBid,
            Ask = nextAsk,
            BidSize = Math.Max(1, current.BidSize + _random.Next(-100, 101)),
            AskSize = Math.Max(1, current.AskSize + _random.Next(-100, 101)),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
