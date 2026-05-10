using Microsoft.AspNetCore.SignalR;
using TickerScout.Backend.Services;

namespace TickerScout.Backend;

public sealed class QuoteHub(SessionStore sessionStore, IServiceProvider serviceProvider) : Hub
{
    private readonly SessionStore _sessionStore = sessionStore;
    private readonly QuoteSimulatorService _quoteSimulatorService = serviceProvider.GetServices<IHostedService>().OfType<QuoteSimulatorService>().First();
    public override async Task OnConnectedAsync()
    {
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
        _quoteSimulatorService.SendSnapshot(sessionId);
        return Task.CompletedTask;
    }
}
