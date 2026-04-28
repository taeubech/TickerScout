using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class AiService : IAiService
{
    public Task<AiPromptResponse> ProcessPromptAsync(AiPromptRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: integrate with an AI provider (e.g. Azure OpenAI, OpenAI) and implement real prompt handling.
        var reply = "AI integration is not yet configured.";
        return Task.FromResult(new AiPromptResponse { Reply = reply });
    }
}
