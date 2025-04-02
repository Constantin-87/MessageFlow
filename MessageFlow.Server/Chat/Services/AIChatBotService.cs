using Azure.AI.OpenAI;
using OpenAI.Chat;
using Azure;
using System.Text;
using MessageFlow.AzureServices.Services;
using MessageFlow.Shared.DTOs;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.Server.Chat.Services
{
    public class AIChatBotService
    {
        private readonly AzureSearchQueryService _searchService;
        private readonly AzureOpenAIClient _openAiClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _gptDeploymentName;

        public AIChatBotService(AzureSearchQueryService searchService, IConfiguration configuration, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _searchService = searchService;

            // ✅ Retrieve OpenAI configuration from Azure Key Vault
            string? endpoint = configuration["azure-gbt-endpoint"];
            string? apiKey = configuration["azure-gbt-deployment-key"];
            string? deploymentName = "MessageFlowGptDeployment";

            // ✅ Ensure required values are not null (fail-fast if missing)
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
            {
                throw new InvalidOperationException("Azure OpenAI configuration is missing. Check Key Vault values.");
            }

            _openAiClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _gptDeploymentName = deploymentName;
        }

        public async Task<(bool answered, string response, string? targetTeam)> HandleUserQueryAsync(string userQuery, string companyId, string conversationId)
        {
            var searchResults = await _searchService.QueryIndexAsync(userQuery, companyId);

            // ✅ Fetch company teams if no search results found
            List<SearchResultDTO> companyTeams = new();
            bool hasSearchResults = searchResults.Any();

            if (!hasSearchResults)
            {
                companyTeams = await _searchService.QueryIndexAsync("company teams", companyId);
            }

            // ✅ Retrieve Chat History for Context
            var chatHistory = await GetRecentMessagesAsync(conversationId, 5); // Last 5 messages

            var chatCompletionsOptions = new ChatCompletionOptions()
            {
                Temperature = 0.7f,
                MaxOutputTokenCount = 500,
                FrequencyPenalty = 0,
                PresencePenalty = 0
            };

            var messages = new List<ChatMessage>();

            // ✅ Add chat history to SystemChatMessage
            string formattedChatHistory = FormatChatHistory(chatHistory);

            if (hasSearchResults)
            {
                messages.Add(new SystemChatMessage($@"
                    You are an AI assistant that provides accurate responses based strictly on indexed company data.
                    Always refer to the provided information and do not speculate.

                    If this is the first interaction, introduce yourself politely and state your purpose.
                    Example:
                        'Hello! I am an AI assistant here to help you with information related to your company. How can I assist you today?'


                    **Chat History:**
                    {formattedChatHistory}

                    Instructions:
                    - If the user asks to speak with a human agent, determine the most relevant team based on the provided data.
                    - Respond strictly in the following JSON format if redirection is needed:
                      {{
                        ""redirect"": true,
                        ""teamId"": ""<team_id_string>""
                      }}
                    - **Ensure `teamId` is always returned as a string (even if it's a number).** Convert numeric values to strings.

                    Relevant Information:
                    {FormatSearchResults(searchResults)}
                "));
            }
            else
            {
                messages.Add(new SystemChatMessage($@"
                    You are an AI assistant that helps users find the right support team.
                    No indexed company data was found for this request.

                    If this is the first interaction, introduce yourself politely and state your purpose.
                    Example:
                        'Hello! I am an AI assistant here to help you with information related to your company. How can I assist you today?'

                    **Chat History:**
                    {formattedChatHistory}

                    Instructions:
                    - Since there is no direct answer available, guide the user.
                    - If you can infer the most relevant team, ask: 
                      'I can't answer your question directly, but would you like me to redirect you to the <Team Name> team?'
                    - If no team can be determined, ask the user: 
                      'I am unable to determine the right team. Which team would you like to be redirected to for further assistance?'
                    - Always respond in this JSON format if redirection is needed:
                      {{
                        ""redirect"": true,
                        ""teamId"": ""<team_id_string>""
                      }}
                    - **Ensure `teamId` is always returned as a string.**

                    Available Company Teams:
                    {FormatSearchResults(companyTeams)}
                "));
            }

            messages.Add(new UserChatMessage($"User Query: {userQuery}"));

            ChatClient chatClient = _openAiClient.GetChatClient(_gptDeploymentName);
            string gbtContentResponse = "";
            string? targetTeamId = null;

            try
            {
                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, chatCompletionsOptions);

                if (completion.Content != null && completion.Content.Count > 0)
                {
                    gbtContentResponse = completion.Content[0].Text;

                    try
                    {
                        var structuredResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(gbtContentResponse);

                        if (structuredResponse != null && structuredResponse.TryGetValue("redirect", out var redirectValue))
                        {
                            if (redirectValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
                            {
                                if (structuredResponse.TryGetValue("teamId", out var teamIdValue) &&
                                    teamIdValue is JsonElement teamIdElement &&
                                    teamIdElement.ValueKind == JsonValueKind.String)
                                {
                                    targetTeamId = teamIdElement.GetString();
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"⚠️ JSON Parsing Error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return (true, gbtContentResponse, targetTeamId);
        }

        private async Task<List<Message>> GetRecentMessagesAsync(string conversationId, int limit)
        {
            return await _unitOfWork.Messages
                .GetMessagesByConversationIdAsync(conversationId, limit);
        }

        private string FormatChatHistory(List<Message> chatHistory)
        {
            if (!chatHistory.Any())
                return "No prior conversation history available.";

            var formattedHistory = new StringBuilder();

            foreach (var message in chatHistory.OrderBy(m => m.SentAt))
            {
                formattedHistory.AppendLine($"[{message.SentAt:HH:mm}] {message.Username}: {message.Content}");
            }

            return formattedHistory.ToString();
        }

        private string FormatSearchResults(List<SearchResultDTO> searchResults)
        {
            var formattedResults = new StringBuilder();

            foreach (var result in searchResults)
            {
                formattedResults.AppendLine($"📌 **Source: {result.FileDescription}**");

                try
                {
                    if (!string.IsNullOrEmpty(result.Content))
                    {
                        // 🔹 Extract JSON part using `content:` as reference
                        int jsonStartIndex = result.Content.IndexOf("content:");
                        if (jsonStartIndex != -1)
                        {
                            string jsonContent = result.Content.Substring(jsonStartIndex + 8).Trim();

                            // 🔹 Ensure we correctly isolate the JSON block
                            int jsonArrayStart = jsonContent.IndexOf("[");
                            int jsonArrayEnd = jsonContent.LastIndexOf("]");
                            int jsonObjectStart = jsonContent.IndexOf("{");
                            int jsonObjectEnd = jsonContent.LastIndexOf("}");

                            if (jsonArrayStart != -1 && jsonArrayEnd != -1)
                            {
                                // Extract JSON Array (`[]`)
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
                                // Extract JSON Object (`{}`)
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
                                formattedResults.AppendLine("⚠️ Could not extract valid JSON content.");
                            }
                        }
                        else
                        {
                            formattedResults.AppendLine("⚠️ No valid JSON found in content.");
                        }
                    }
                    else
                    {
                        formattedResults.AppendLine("⚠️ No content available.");
                    }
                }
                catch (JsonException ex)
                {
                    formattedResults.AppendLine($"⚠️ Error parsing content: {ex.Message}");
                }

                formattedResults.AppendLine("------\n"); // Separator
            }

            return formattedResults.ToString();
        }



        private string ConvertJsonElementToString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.ToString(), // Converts numbers safely
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                _ => element.ToString() // Fallback for unexpected cases
            };
        }




    }
}

