namespace TickerScout.Backend;

public sealed class AiOptions
{
    public string Endpoint { get; set; } = "https://tickerscout-agent-resource.services.ai.azure.com/api/projects/tickerscout-agent";

    public string ModelDeploymentName { get; set; } = "gpt-4o";

    public string AgentName { get; set; } = "tickerscout-agent";

    public string? Username { get; set; }

    public string? AccessToken { get; set; }
}
