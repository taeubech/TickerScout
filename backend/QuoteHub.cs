using Microsoft.AspNetCore.SignalR;
using TickerScout.Backend.Models;
using TickerScout.Backend.Services;

namespace TickerScout.Backend;

public sealed class QuoteHub(QuoteStore quoteStore, SessionStore sessionStore) : Hub
{
    private readonly QuoteStore _quoteStore = quoteStore;
    private readonly SessionStore _sessionStore = sessionStore;

    public override async Task OnConnectedAsync()
    {
        _sessionStore.AddConnection(Context.ConnectionId);
        await Clients.Caller.SendAsync("ReceiveSnapshot", _quoteStore.GetSnapshot());
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _sessionStore.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public Task RegisterSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new HubException("SessionId must not be empty.");
        }

        var session = _sessionStore.GetOrCreate(sessionId);
        _sessionStore.AssociateConnection(Context.ConnectionId, session.SessionId);
        return Task.CompletedTask;
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
