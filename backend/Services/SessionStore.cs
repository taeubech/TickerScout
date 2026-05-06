using System.Collections.Concurrent;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class SessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _connectionToSession = new(StringComparer.OrdinalIgnoreCase);

    public Session GetOrCreate(string sessionId)
    {
        var session = _sessions.AddOrUpdate(
            sessionId,
            id => new Session { SessionId = id },
            (_, existing) =>
            {
                existing.LastSeenAt = DateTimeOffset.UtcNow;
                return existing;
            });

        return session;
    }

    public void AddConnection(string connectionId)
    {
        _connectionToSession[connectionId] = null!;
    }

    public void AssociateConnection(string connectionId, string sessionId)
    {
        _connectionToSession[connectionId] = sessionId;
    }

    public void RemoveConnection(string connectionId)
    {
        _connectionToSession.TryRemove(connectionId, out _);
        OnConnectionRemoved?.Invoke(connectionId);
    }

    public IEnumerable<string> GetAllConnectionIds() => _connectionToSession.Keys;

    public Session? GetByConnectionId(string connectionId)
    {
        if (_connectionToSession.TryGetValue(connectionId, out var sessionId) &&
            _sessions.TryGetValue(sessionId, out var session))
        {
            return session;
        }

        return null;
    }

    public Session? GetBySessionId(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var session) ? session : null;

    public event Action<string>? OnConnectionRemoved;
}
