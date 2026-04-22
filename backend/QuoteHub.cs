using Microsoft.AspNetCore.SignalR;
using TickerScout.Backend.Models;
using TickerScout.Backend.Services;

namespace TickerScout.Backend;

public sealed class QuoteHub(QuoteStore quoteStore) : Hub
{
    private readonly QuoteStore _quoteStore = quoteStore;

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("ReceiveSnapshot", _quoteStore.GetSnapshot());
        await base.OnConnectedAsync();
    }

    public async Task UpdateQuote(QuoteEdit update)
    {
        var quote = _quoteStore.ApplyClientEdit(update);
        if (quote is null)
        {
            return;
        }

        await Clients.All.SendAsync("ReceiveQuote", quote);
    }
}
