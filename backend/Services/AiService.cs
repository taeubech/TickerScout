using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class AiService(
    SessionStore sessionStore,
    IQuoteFilterService quoteFilterService,
    IOptions<AiOptions> aiOptions) : IAiService
{
#pragma warning disable OPENAI001

    private static readonly FunctionTool SetFiltersTool = ResponseTool.CreateFunctionTool(
        functionName: "set_filters",
        functionDescription: "Sets the quote filter for the current session. Only quotes that match that filter will be displayed. Call this whenever the user asks to filter, show, or restrict quotes, stocks, prices, instruments or similar by any field. An empty filter (null) will show all quotes.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "filter": {
              "description": "The filter to apply. Can be a simple filter or a combination using And/Or/Not.",
              "oneOf": [
                {
                  "type": "object",
                  "description": "Filter quotes by instrument type(s)",
                  "properties": {
                    "type": { "const": "InstrumentType" },
                    "instrumentTypes": {
                      "type": "array",
                      "items": { "type": "string", "enum": ["Stock", "Future", "ETF"] },
                      "description": "List of instrument types to include"
                    }
                  },
                  "required": ["type", "instrumentTypes"],
                  "additionalProperties": false
                },
                {
                  "type": "object",
                  "description": "Filter quotes by symbol(s)",
                  "properties": {
                    "type": { "const": "Symbol" },
                    "symbols": {
                      "type": "array",
                      "items": { "type": "string" },
                      "description": "List of symbols to include (e.g., ['AAPL', 'MSFT'])"
                    }
                  },
                  "required": ["type", "symbols"],
                  "additionalProperties": false
                },
                {
                  "type": "object",
                  "description": "Filter quotes by currency(ies)",
                  "properties": {
                    "type": { "const": "Currency" },
                    "currencies": {
                      "type": "array",
                      "items": { "type": "string" },
                      "description": "List of currencies to include (e.g., ['USD', 'EUR'])"
                    }
                  },
                  "required": ["type", "currencies"],
                  "additionalProperties": false
                },
                        {
                  "type": "object",
                  "description": "Filter quotes where Last price is greater than a threshold",
                  "properties": {
                    "type": { "const": "LastGreaterThan" },
                    "threshold": {
                      "type": "number",
                      "description": "Minimum Last price value"
                    }
                  },
                  "required": ["type", "threshold"],
                  "additionalProperties": false
                },
                {
                  "type": "object",
                  "description": "Logical NOT - inverts the inner filter",
                  "properties": {
                    "type": { "const": "Not" },
                    "innerFilter": {
                      "description": "The filter to negate"
                    }
                  },
                  "required": ["type", "innerFilter"],
                  "additionalProperties": false
                },
                {
                  "type": "object",
                  "description": "Logical AND - both filters must match",
                  "properties": {
                    "type": { "const": "And" },
                    "filter1": {
                      "description": "First filter condition"
                    },
                    "filter2": {
                      "description": "Second filter condition"
                    }
                  },
                  "required": ["type", "filter1", "filter2"],
                  "additionalProperties": false
                },
                {
                  "type": "object",
                  "description": "Logical OR - at least one filter must match",
                  "properties": {
                    "type": { "const": "Or" },
                    "filter1": {
                      "description": "First filter condition"
                    },
                    "filter2": {
                      "description": "Second filter condition"
                    }
                  },
                  "required": ["type", "filter1", "filter2"],
                  "additionalProperties": false
                }
              ]
            }
          },
          "required": ["filter"]
        }
        """),
        strictModeEnabled: false);

    public Task<AiPromptResponse> ProcessPromptAsync(AiPromptRequest request, CancellationToken cancellationToken = default)
    {
        AIProjectClient projectClient = CreateProjectClient();
        AiOptions options = aiOptions.Value;

        // Create your agent with the SetFilters function tool
        DeclarativeAgentDefinition agentDefinition = new(model: options.ModelDeploymentName)
        {
            Tools = { SetFiltersTool }
        };

        // Creates an agent or bumps the existing agent version if parameters have changed
        ClientResult<ProjectsAgentVersion> clientResult = projectClient.AgentAdministrationClient.CreateAgentVersion(
            agentName: options.AgentName,
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
            var responseOptions = new CreateResponseOptions();
            foreach (var item in inputItems)
                responseOptions.InputItems.Add(item);

            response = responseClient.CreateResponse(responseOptions);
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

    private AIProjectClient CreateProjectClient()
    {
        AiOptions options = aiOptions.Value;

        if (string.IsNullOrWhiteSpace(options.Username) && string.IsNullOrWhiteSpace(options.AccessToken))
        {
            return new AIProjectClient(endpoint: new Uri(options.Endpoint), tokenProvider: new DefaultAzureCredential());
        }

        if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.AccessToken))
        {
            throw new InvalidOperationException("Both Ai:Username and Ai:AccessToken must be configured together.");
        }

        string basicCredential = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.AccessToken}"));
        AuthenticationPolicy authenticationPolicy = ApiKeyAuthenticationPolicy.CreateBasicAuthorizationPolicy(new ApiKeyCredential(basicCredential));

        return new AIProjectClient(authenticationPolicy, new Uri(options.Endpoint), new AIProjectClientOptions());
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
            using (JsonDocument argsDoc = JsonDocument.Parse(functionCall.FunctionArguments))
            {
                var filterJsonElement = argsDoc.RootElement.GetProperty("filter");
                var filter = JsonToQuoteFilter(filterJsonElement);
                quoteFilterService.SetFilters(sessionId, filter);
            }

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

    private QuoteFilter JsonToQuoteFilter(JsonElement jsonElement)
    {
        QuoteFilter filter = jsonElement.GetProperty("type").GetString() switch
        {
            "instrumentTypes" => new InstrumentTypeFilter([.. jsonElement.GetProperty("instrumentTypes").EnumerateArray().Select(e => Enum.Parse<InstrumentType>(e.GetString()!))]),
            "symbols" => new SymbolFilter([.. jsonElement.GetProperty("symbols").EnumerateArray().Select(e => e.GetString()!)]),
            "threshold" => new LastGreaterThanFilter(jsonElement.GetProperty("threshold").GetDouble()),
            "currencies" => new CurrencyFilter([.. jsonElement.GetProperty("currencies").EnumerateArray().Select(e => e.GetString()!)]),
            "Not" => new NotFilter(JsonToQuoteFilter(jsonElement.GetProperty("innerFilter"))),
            "And" => new AndFilter(JsonToQuoteFilter(jsonElement.GetProperty("filter1")), JsonToQuoteFilter(jsonElement.GetProperty("filter2"))),
            "Or" => new OrFilter(JsonToQuoteFilter(jsonElement.GetProperty("filter1")), JsonToQuoteFilter(jsonElement.GetProperty("filter2"))),
            _ => throw new Exception($"Unknown filter type: {jsonElement.GetProperty("type").GetString()}")
        };
        return filter;
    }
}
