using Azure.AI.OpenAI;
using Azure;
using MediatR;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.AiBotProcessing.Commands;
using MessageFlow.Shared.DTOs;
using OpenAI.Chat;
using System.Text.Json;
using System.Text;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.DataTransferObjects.Internal;
using MessageFlow.AzureServices.Interfaces;

namespace MessageFlow.Server.MediatR.Chat.AiBotProcessing.CommandHandlers
{
    public class HandleUserQueryHandler : IRequestHandler<HandleUserQueryCommand, UserQueryResponseDTO>
    {
        private readonly IAzureSearchQueryService _searchService;
        private readonly AzureOpenAIClient _openAiClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _deployment;
        private readonly ILogger<HandleUserQueryHandler> _logger;

        public HandleUserQueryHandler(
            IAzureSearchQueryService searchService,
            IConfiguration config,
            IUnitOfWork unitOfWork,
            ILogger<HandleUserQueryHandler> logger)
        {
            _searchService = searchService;
            _unitOfWork = unitOfWork;
            _logger = logger;

            var endpoint = config["azure-gbt-endpoint"];
            var key = config["azure-gbt-deployment-key"];
            _deployment = "MessageFlowGptDeployment";

            _openAiClient = new AzureOpenAIClient(new Uri(endpoint!), new AzureKeyCredential(key!));
        }

        public async Task<UserQueryResponseDTO> Handle(HandleUserQueryCommand req, CancellationToken ct)
        {
            var searchResults = await _searchService.QueryIndexAsync(req.UserQuery, req.CompanyId);
            var hasResults = searchResults.Any();
            var fallbackTeams = hasResults ? new List<SearchResultDTO>() :
                await _searchService.QueryIndexAsync("company teams", req.CompanyId);

            var chatHistory = await _unitOfWork.Messages.GetMessagesByConversationIdAsync(req.ConversationId, 5);
            var historyStr = FormatChatHistory(chatHistory);

            var sysPrompt = hasResults
                ? BuildSystemPromptWithResults(historyStr, searchResults)
                : BuildSystemPromptNoResults(historyStr, fallbackTeams);

            var chatClient = _openAiClient.GetChatClient(_deployment);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(sysPrompt),
                new UserChatMessage($"User Query: {req.UserQuery}")
            };

            try
            {
                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
                {
                    Temperature = 0.7f,
                    MaxOutputTokenCount = 500
                });

                var content = completion.Content?.FirstOrDefault()?.Text ?? "";
                var (redirect, teamId, teamName) = TryExtractRedirect(content);
                return new UserQueryResponseDTO
                {
                    Answered = true,
                    RawResponse = content,
                    TargetTeamId = redirect ? teamId : null,
                    TargetTeamName = redirect ? teamName : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GPT error while handling query '{UserQuery}' for company '{CompanyId}'", req.UserQuery, req.CompanyId);
                return new UserQueryResponseDTO
                {
                    Answered = false,
                    RawResponse = "",
                    TargetTeamId = null,
                    TargetTeamName = null
                };
            }
        }

        private static (bool Redirect, string? TeamId, string? TeamName) TryExtractRedirect(string json)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (parsed?["redirect"].GetBoolean() == true)
                {
                    var id = parsed["teamId"].GetString();
                    var name = parsed.TryGetValue("teamName", out var nameElement) ? nameElement.GetString() : null;
                    return (true, id, name);
                }
            }
            catch { }
            return (false, null, null);
        }

        private static string FormatChatHistory(IEnumerable<Message> msgs)
        {
            if (!msgs.Any()) return "No prior conversation history.";
            return string.Join("\n", msgs.OrderBy(m => m.SentAt)
                .Select(m => $"[{m.SentAt:HH:mm}] {m.Username}: {m.Content}"));
        }

        private static string BuildSystemPromptWithResults(string history, List<SearchResultDTO> results)
        {
            var sb = new StringBuilder();

            sb.AppendLine("""
                You are an AI assistant that provides accurate responses based strictly on indexed company data.
                Always refer to the provided information and do not speculate.

                If this is the first interaction, introduce yourself politely and state your purpose.
                Example:
                    'Hello! I am an AI assistant here to help you with information related to your company. How can I assist you today?'

                **Chat History:**
                """);
            sb.AppendLine(history);
            sb.AppendLine("""
        
                Instructions:
                - If the user asks to speak with a human agent, determine the most relevant team based on the provided data.
                - Respond strictly in the following JSON format if redirection is needed:
                  {
                    "redirect": true,
                    "teamId": "<team_id_string>",
                    "teamName": "<team_name>"
                  }
                - **Ensure `teamId` is always returned as a string (even if it's a number).** Convert numeric values to strings.

                Relevant Information:
                """);

            sb.AppendLine(FormatSearchResults(results));
            return sb.ToString();
        }

        private static string BuildSystemPromptNoResults(string history, List<SearchResultDTO> fallbackTeams)
        {
            var sb = new StringBuilder();

            sb.AppendLine("""
                You are an AI assistant that helps users find the right support team.
                No indexed company data was found for this request.

                If this is the first interaction, introduce yourself politely and state your purpose.
                Example:
                    'Hello! I am an AI assistant here to help you with information related to your company. How can I assist you today?'

                **Chat History:**
                """);
            sb.AppendLine(history);
            sb.AppendLine("""
        
                Instructions:
                - Since there is no direct answer available, guide the user.
                - If you can infer the most relevant team, ask: 
                  'I can't answer your question directly, but would you like me to redirect you to the <Team Name> team?'
                - If no team can be determined, ask the user: 
                  'I am unable to determine the right team. Which team would you like to be redirected to for further assistance?'
                - Always respond in this JSON format if redirection is needed:
                  {
                    "redirect": true,
                    "teamId": "<team_id_string>",
                    "teamName": "<team_name>"
                  }
                - **Ensure `teamId` is always returned as a string.**

                Available Company Teams:
                """);

            sb.AppendLine(FormatSearchResults(fallbackTeams));
            return sb.ToString();
        }

        private static string FormatSearchResults(List<SearchResultDTO> searchResults)
        {
            var formattedResults = new StringBuilder();

            foreach (var result in searchResults)
            {
                formattedResults.AppendLine($"**Source: {result.FileDescription}**");

                try
                {
                    if (!string.IsNullOrEmpty(result.Content))
                    {
                        // Extract JSON part
                        int jsonStartIndex = result.Content.IndexOf("content:");
                        if (jsonStartIndex != -1)
                        {
                            string jsonContent = result.Content.Substring(jsonStartIndex + 8).Trim();

                            // Isolate the JSON block
                            int jsonArrayStart = jsonContent.IndexOf("[");
                            int jsonArrayEnd = jsonContent.LastIndexOf("]");
                            int jsonObjectStart = jsonContent.IndexOf("{");
                            int jsonObjectEnd = jsonContent.LastIndexOf("}");

                            if (jsonArrayStart != -1 && jsonArrayEnd != -1)
                            {
                                // Extract JSON Array
                                jsonContent = jsonContent.Substring(jsonArrayStart, jsonArrayEnd - jsonArrayStart + 1);

                                var contentArray = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(jsonContent);

                                if (contentArray != null)
                                {
                                    foreach (var item in contentArray)
                                    {
                                        formattedResults.AppendLine("🔹 Contact Entry:");
                                        foreach (var (key, value) in item)
                                        {
                                            formattedResults.AppendLine($"  - **{key}**: {ConvertJsonElementToString(value)}");
                                        }
                                    }
                                }
                            }
                            else if (jsonObjectStart != -1 && jsonObjectEnd != -1)
                            {
                                // Extract JSON Object
                                jsonContent = jsonContent.Substring(jsonObjectStart, jsonObjectEnd - jsonObjectStart + 1);

                                var contentJson = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

                                if (contentJson != null)
                                {
                                    foreach (var (key, value) in contentJson)
                                    {
                                        formattedResults.AppendLine($"- **{key}**: {ConvertJsonElementToString(value)}");
                                    }
                                }
                            }
                            else
                            {
                                formattedResults.AppendLine("Could not extract valid JSON content.");
                            }
                        }
                        else
                        {
                            formattedResults.AppendLine("No valid JSON found in content.");
                        }
                    }
                    else
                    {
                        formattedResults.AppendLine("No content available.");
                    }
                }
                catch (JsonException ex)
                {
                    formattedResults.AppendLine($"Error parsing content: {ex.Message}");
                }

                formattedResults.AppendLine("------\n");
            }

            return formattedResults.ToString();
        }

        private static string ConvertJsonElementToString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                _ => element.ToString() // Fallback for unexpected cases
            };
        }
    }
}
