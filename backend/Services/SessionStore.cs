using System.Collections.Concurrent;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class SessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessionsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _connectionIdsBySessionId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _sessionIdsByConnectionId = new(StringComparer.OrdinalIgnoreCase);

    public Session GetOrCreate(string sessionId)
    {
        var session = _sessionsById.AddOrUpdate(
            sessionId,
            id => new Session { SessionId = id },
            (_, existing) =>
            {
                existing.LastSeenAt = DateTimeOffset.UtcNow;
                return existing;
            });

        return session;
    }

    public void AssociateConnection(string connectionId, string sessionId)
    {
        if (_sessionIdsByConnectionId.TryGetValue(connectionId, out var previousSessionId) &&
            !string.Equals(previousSessionId, sessionId, StringComparison.OrdinalIgnoreCase))
        {
            _connectionIdsBySessionId.TryRemove(previousSessionId, out _);
        }

        if (_connectionIdsBySessionId.TryGetValue(sessionId, out var previousConnectionId) &&
            !string.Equals(previousConnectionId, connectionId, StringComparison.OrdinalIgnoreCase))
        {
            _sessionIdsByConnectionId.TryRemove(previousConnectionId, out _);
        }

        _sessionIdsByConnectionId[connectionId] = sessionId;
        _connectionIdsBySessionId[sessionId] = connectionId;

        if (_sessionsById.TryGetValue(sessionId, out var session))
        {
            session.LastSeenAt = DateTimeOffset.UtcNow;
        }
    }

    public void RemoveConnection(string connectionId)
    {
        if (!_sessionIdsByConnectionId.TryRemove(connectionId, out var sessionId))
        {
            return;
        }

        if (_connectionIdsBySessionId.TryGetValue(sessionId, out var activeConnectionId) &&
            string.Equals(activeConnectionId, connectionId, StringComparison.OrdinalIgnoreCase))
        {
            _connectionIdsBySessionId.TryRemove(sessionId, out _);
        }

        if (_sessionsById.TryGetValue(sessionId, out var session))
        {
            session.LastSeenAt = DateTimeOffset.UtcNow;
        }

        SessionDisconnected?.Invoke(sessionId);
    }

    public IEnumerable<string> GetConnectedSessionIds() => _connectionIdsBySessionId.Keys;

    public Session? GetByConnectionId(string connectionId)
    {
        if (_sessionIdsByConnectionId.TryGetValue(connectionId, out var sessionId) &&
            _sessionsById.TryGetValue(sessionId, out var session))
        {
            return session;
        }

        return null;
    }

    public bool TryGetConnectionId(string sessionId, out string connectionId) =>
        _connectionIdsBySessionId.TryGetValue(sessionId, out connectionId!);

    public event Action<string>? SessionDisconnected;
}
