using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Responses;
using System.ClientModel;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class AiService(SessionStore sessionStore) : IAiService
{
#pragma warning disable OPENAI001

    const string endpoint = "https://tickerscout-agent-resource.services.ai.azure.com/api/projects/tickerscout-agent";
    const string modelDeploymentName = "gpt-4o";
    const string agentName = "tickerscout-agent";

    public Task<AiPromptResponse> ProcessPromptAsync(AiPromptRequest request, CancellationToken cancellationToken = default)
    {
        // The AzureCliCredential will use your logged-in Azure CLI identity, make sure to run `az login` first
        AIProjectClient projectClient = new(endpoint: new Uri(endpoint), tokenProvider: new DefaultAzureCredential());

        // Create your agent
        ProjectsAgentDefinition agentDefinition = ProjectsAgentDefinition.CreatePromptAgentDefinition(model: modelDeploymentName);

        // Creates an agent or bumps the existing agent version if parameters have changed
        ClientResult<ProjectsAgentVersion> clientResult = projectClient.AgentAdministrationClient.CreateAgentVersion(
            agentName: agentName,
            options: new(agentDefinition));
        var agentVersion = clientResult.Value;
        Console.WriteLine($"Agent created (id: {agentVersion.Id}, name: {agentVersion.Name}, version: {agentVersion.Version})");

        // Resolve or create the conversation ID for this session.
        // When a SessionId is provided and a conversation already exists for that session,
        // the existing conversation is reused so that message history is preserved.
        string conversationId = ResolveConversationId(projectClient, request.SessionId);

        ProjectResponsesClient responseClient
            = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new(name: agentVersion.Name, version: agentVersion.Version), conversationId);
        // Use the agent to generate a response
        ResponseResult response = responseClient.CreateResponse(request.Prompt);

        return Task.FromResult(new AiPromptResponse { Reply = response.GetOutputText() });
    }

    private string ResolveConversationId(AIProjectClient projectClient, string? sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId))
        {
            Session session = sessionStore.GetOrCreate(sessionId);

            if (session.AiConversationId is not null)
            {
                return session.AiConversationId;
            }

            // No conversation yet for this session – create one and persist its ID.
            ProjectConversation conversation = projectClient.ProjectOpenAIClient.GetProjectConversationsClient().CreateProjectConversation();
            session.AiConversationId = conversation.Id;
            return conversation.Id;
        }

        // No session context: fall back to a transient, single-use conversation.
        return projectClient.ProjectOpenAIClient.GetProjectConversationsClient().CreateProjectConversation().Value.Id;
    }
}
