using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public interface IAiService
{
    Task<AiPromptResponse> ProcessPromptAsync(AiPromptRequest request, CancellationToken cancellationToken = default);
}
