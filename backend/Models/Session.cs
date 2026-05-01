namespace TickerScout.Backend.Models;

public sealed class Session
{
    public required string SessionId { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    public string? AiConversationId { get; set; }
}
