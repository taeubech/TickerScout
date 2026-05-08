using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Responses;
using System.ClientModel;
using System.Text.Json;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class AiService(SessionStore sessionStore, IQuoteFilterService quoteFilterService) : IAiService
{
#pragma warning disable OPENAI001

    const string endpoint = "https://tickerscout-agent-resource.services.ai.azure.com/api/projects/tickerscout-agent";
    const string modelDeploymentName = "gpt-4o";
    const string agentName = "tickerscout-agent";

    private static readonly FunctionTool SetFiltersTool = ResponseTool.CreateFunctionTool(
        functionName: "set_filters",
        functionDescription: "Sets the quote filters for the current session. Only quotes matching at least one filter will be displayed. Call this whenever the user asks to filter, show, or restrict quotes by any field.",
        functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "filters": {
                  "type": "array",
                  "description": "List of quote filters to apply. A quote passes if it matches at least one filter.",
                  "items": {
                    "type": "object",
                    "properties": {
                      "field": {
                        "type": "string",
                        "enum": ["Symbol", "Bid", "Ask", "Last", "Open", "Close", "BidSize", "AskSize", "InstrumentType"],
                        "description": "The quote field to filter on."
                      },
                      "operator": {
                        "type": "string",
                        "enum": ["Equals", "NotEquals", "GreaterThan", "GreaterThanOrEquals", "LessThan", "LessThanOrEquals", "Contains"],
                        "description": "The comparison operator to apply."
                      },
                      "value": {
                        "type": "string",
                        "description": "The value to compare against. For InstrumentType fields, only the values 'Stock', 'Future', 'ETF' or null are allowed. Null means don't filter for any instrument type."
                      }
                    },
                    "required": ["field", "operator", "value"]
                  }
                }
              },
              "required": ["filters"]
            }
            """),
        strictModeEnabled: false);

    public Task<AiPromptResponse> ProcessPromptAsync(AiPromptRequest request, CancellationToken cancellationToken = default)
    {
        // The AzureCliCredential will use your logged-in Azure CLI identity, make sure to run `az login` first
        AIProjectClient projectClient = new(endpoint: new Uri(endpoint), tokenProvider: new DefaultAzureCredential());

        // Create your agent with the SetFilters function tool
        DeclarativeAgentDefinition agentDefinition = new(model: modelDeploymentName)
        {
            Tools = { SetFiltersTool }
        };

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

        // Run the prompt through a tool-call loop so that any set_filters invocations
        // issued by the model are executed locally before returning the final reply.
        List<ResponseItem> inputItems = [ResponseItem.CreateUserMessageItem(request.Prompt)];
        bool toolCallMade;
        ResponseResult response;
        do
        {
            var options = new CreateResponseOptions();
            foreach (var item in inputItems)
                options.InputItems.Add(item);

            response = responseClient.CreateResponse(options);
            toolCallMade = false;
            foreach (ResponseItem outputItem in response.OutputItems)
            {
                inputItems.Add(outputItem);
                if (outputItem is FunctionCallResponseItem functionCall)
                {
                    inputItems.Add(ResolveToolCall(functionCall, request.SessionId));
                    toolCallMade = true;
                }
            }
        } while (toolCallMade);

        return Task.FromResult(new AiPromptResponse { Reply = response.GetOutputText() });
    }

    private FunctionCallOutputResponseItem ResolveToolCall(FunctionCallResponseItem functionCall, string? sessionId)
    {
        if (functionCall.FunctionName != SetFiltersTool.FunctionName)
        {
            return ResponseItem.CreateFunctionCallOutputItem(functionCall.CallId, "error: unknown function");
        }

        if (string.IsNullOrEmpty(sessionId))
        {
            return ResponseItem.CreateFunctionCallOutputItem(functionCall.CallId, "error: no active session");
        }

        try
        {
            List<QuoteFilter> filters;
            using (JsonDocument argsDoc = JsonDocument.Parse(functionCall.FunctionArguments))
            {
                filters = argsDoc.RootElement.GetProperty("filters").EnumerateArray()
                    .Select(f => new QuoteFilter
                    {
                        Field = Enum.Parse<QuoteField>(f.GetProperty("field").GetString()!),
                        Operator = Enum.Parse<FilterOperator>(f.GetProperty("operator").GetString()!),
                        Value = f.GetProperty("value").GetString()!
                    })
                    .ToList();
            }

            quoteFilterService.SetFilters(sessionId, filters);
            return ResponseItem.CreateFunctionCallOutputItem(functionCall.CallId, "Filters applied successfully.");
        }
        catch (Exception ex)
        {
            return ResponseItem.CreateFunctionCallOutputItem(functionCall.CallId, $"error: {ex.Message}");
        }
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
