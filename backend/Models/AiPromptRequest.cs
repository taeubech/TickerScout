using System.ComponentModel.DataAnnotations;

namespace TickerScout.Backend.Models;

public sealed class AiPromptRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string Prompt { get; init; }

    public string? SessionId { get; init; }
}
