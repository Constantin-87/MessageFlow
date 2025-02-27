//using Azure.AI.OpenAI;
//using OpenAI.Chat;
//using Azure;
//using System.Text;
//using MessageFlow.AzureServices.Services;
//using MessageFlow.Shared.DTOs;
//using System.Text.Json;
//using Microsoft.EntityFrameworkCore;
//using MessageFlow.Server.Models;
//using MessageFlow.Server.Data;

//namespace MessageFlow.Server.Components.Chat.Services
//{
//    public class AIChatBotService
//    {
//        private readonly AzureSearchQueryService _searchService;
//        private readonly AzureOpenAIClient _openAiClient;
//        private readonly ApplicationDbContext _dbContext;
//        private readonly string _gptDeploymentName;

//        public AIChatBotService(AzureSearchQueryService searchService, IConfiguration configuration, ApplicationDbContext dbContext)
//        {
//            _searchService = searchService;

//            // ✅ Retrieve OpenAI configuration from Azure Key Vault
//            string? endpoint = configuration["azure-gbt-endpoint"];
//            string? apiKey = configuration["azure-gbt-deployment-key"];
//            string? deploymentName = "MessageFlowGptDeployment";

//            // ✅ Ensure required values are not null (fail-fast if missing)
//            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
//            {
//                throw new InvalidOperationException("Azure OpenAI configuration is missing. Check Key Vault values.");
//            }

//            _openAiClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
//            _gptDeploymentName = deploymentName;
//            _dbContext = dbContext;
//        }

//        public async Task<(bool answered, string response, string? targetTeam)> HandleUserQueryAsync(string userQuery, string companyId, string conversationId)
//        {
//            int companyIdInt;
//            if (int.TryParse(companyId, out int parsedCompanyId))
//            {
//                companyIdInt = parsedCompanyId;
//            }
//            else
//            {
//                throw new ArgumentException("Invalid company ID. Expected a valid integer.", nameof(companyId));
//            }

//            var searchResults = await _searchService.QueryIndexAsync(userQuery, companyIdInt);

//            // ✅ Fetch company teams if no search results found
//            List<SearchResult> companyTeams = new();
//            bool hasSearchResults = searchResults.Any();

//            if (!hasSearchResults)
//            {
//                companyTeams = await _searchService.QueryIndexAsync("company teams", companyIdInt);
//            }

//            // ✅ Retrieve Chat History for Context
//            var chatHistory = await GetRecentMessagesAsync(conversationId, 5); // Last 5 messages

//            var chatCompletionsOptions = new ChatCompletionOptions()
//            {
//                Temperature = 0.7f,
//                MaxOutputTokenCount = 500,
//                FrequencyPenalty = 0,
//                PresencePenalty = 0
//            };

//            var messages = new List<ChatMessage>();

//            // ✅ Add chat history to SystemChatMessage
//            string formattedChatHistory = FormatChatHistory(chatHistory);

//            if (hasSearchResults)
//            {
//                messages.Add(new SystemChatMessage($@"
//                    You are an AI assistant that provides accurate responses based strictly on indexed company data.
//                    Always refer to the provided information and do not speculate.

//                    If this is the first interaction, introduce yourself politely and state your purpose.
//                    Example:
//                        'Hello! I am an AI assistant here to help you with information related to your company. How can I assist you today?'


//                    **Chat History:**
//                    {formattedChatHistory}

//                    Instructions:
//                    - If the user asks to speak with a human agent, determine the most relevant team based on the provided data.
//                    - Respond strictly in the following JSON format if redirection is needed:
//                      {{
//                        ""redirect"": true,
//                        ""teamId"": ""<team_id_string>""
//                      }}
//                    - **Ensure `teamId` is always returned as a string (even if it's a number).** Convert numeric values to strings.

//                    Relevant Information:
//                    {FormatSearchResults(searchResults)}
//                "));
//            }
//            else
//            {
//                messages.Add(new SystemChatMessage($@"
//                    You are an AI assistant that helps users find the right support team.
//                    No indexed company data was found for this request.

//                    If this is the first interaction, introduce yourself politely and state your purpose.
//                    Example:
//                        'Hello! I am an AI assistant here to help you with information related to your company. How can I assist you today?'

//                    **Chat History:**
//                    {formattedChatHistory}

//                    Instructions:
//                    - Since there is no direct answer available, guide the user.
//                    - If you can infer the most relevant team, ask: 
//                      'I can't answer your question directly, but would you like me to redirect you to the <Team Name> team?'
//                    - If no team can be determined, ask the user: 
//                      'I am unable to determine the right team. Which team would you like to be redirected to for further assistance?'
//                    - Always respond in this JSON format if redirection is needed:
//                      {{
//                        ""redirect"": true,
//                        ""teamId"": ""<team_id_string>""
//                      }}
//                    - **Ensure `teamId` is always returned as a string.**

//                    Available Company Teams:
//                    {FormatSearchResults(companyTeams)}
//                "));
//            }

//            messages.Add(new UserChatMessage($"User Query: {userQuery}"));

//            ChatClient chatClient = _openAiClient.GetChatClient(_gptDeploymentName);
//            string gbtContentResponse = "";
//            string? targetTeamId = null;

//            try
//            {
//                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, chatCompletionsOptions);

//                if (completion.Content != null && completion.Content.Count > 0)
//                {
//                    gbtContentResponse = completion.Content[0].Text;

//                    try
//                    {
//                        var structuredResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(gbtContentResponse);

//                        if (structuredResponse != null && structuredResponse.TryGetValue("redirect", out var redirectValue))
//                        {
//                            if (redirectValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
//                            {
//                                if (structuredResponse.TryGetValue("teamId", out var teamIdValue) &&
//                                    teamIdValue is JsonElement teamIdElement &&
//                                    teamIdElement.ValueKind == JsonValueKind.String)
//                                {
//                                    targetTeamId = teamIdElement.GetString();
//                                }
//                            }
//                        }
//                    }
//                    catch (JsonException ex)
//                    {
//                        Console.WriteLine($"⚠️ JSON Parsing Error: {ex.Message}");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"An error occurred: {ex.Message}");
//            }

//            return (true, gbtContentResponse, targetTeamId);
//        }

//        private async Task<List<Message>> GetRecentMessagesAsync(string conversationId, int limit)
//        {
//            return await _dbContext.Messages
//                .Where(m => m.ConversationId == conversationId) // ✅ Filter by Conversation ID
//                .OrderByDescending(m => m.SentAt) // Sort by most recent messages first
//                .Take(limit) // Limit number of messages
//                .ToListAsync();
//        }

//        private string FormatChatHistory(List<Message> chatHistory)
//        {
//            if (!chatHistory.Any())
//                return "No prior conversation history available.";

//            var formattedHistory = new StringBuilder();

//            foreach (var message in chatHistory.OrderBy(m => m.SentAt))
//            {
//                formattedHistory.AppendLine($"[{message.SentAt:HH:mm}] {message.Username}: {message.Content}");
//            }

//            return formattedHistory.ToString();
//        }



//        //        public async Task<(bool answered, string response, string? targetTeam)> HandleUserQueryAsync(string userQuery, int companyId)
//        //        {
//        //            var searchResults = await _searchService.QueryIndexAsync(userQuery, companyId);

//        //            if (!searchResults.Any())

//        //                // 1 have GBT ask _searchService for the company team data so he can redirect or ask for different content
//        //                // 2 Serve Company teams details to GBT and have it redirect

//        //                return (false, "No relevant information found. Escalating to human agent.", null);

//        //            var chatCompletionsOptions = new ChatCompletionOptions()
//        //            {
//        //                Temperature = 0.7f,
//        //                MaxOutputTokenCount = 500,
//        //                FrequencyPenalty = 0,
//        //                PresencePenalty = 0
//        //            };            

//        //            var messages = new List<ChatMessage>
//        //{
//        //                new SystemChatMessage($@"
//        //                    You are an AI assistant that provides accurate responses based strictly on indexed company data.
//        //                    Always refer to the provided information and do not speculate.

//        //                    Instructions:
//        //                    - If the user asks to speak with a human agent, determine the most relevant team based on the provided data.
//        //                    - Respond strictly in the following JSON format if redirection is needed:
//        //                     {{
//        //                        ""redirect"": true,
//        //                        ""teamId"": ""<team_id_string>""
//        //                      }}
//        //                    - **Ensure `teamId` is always returned as a string (even if it's a number).** Convert numeric values to strings.
//        //                    - If no redirection is needed, provide a natural text-based response.

//        //                    Relevant Information:
//        //                    {FormatSearchResults(searchResults)}
//        //                "),

//        //                new UserChatMessage($"User Query: {userQuery}")
//        //            };


//        //            ChatClient chatClient = _openAiClient.GetChatClient(_gptDeploymentName);
//        //            string gbtContentResponse = "";
//        //            string? targetTeamId = null;

//        //            try
//        //            {
//        //                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, chatCompletionsOptions);

//        //                if (completion.Content != null && completion.Content.Count > 0)
//        //                {
//        //                    gbtContentResponse = completion.Content[0].Text;

//        //                        try
//        //                        {
//        //                            var structuredResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(gbtContentResponse);

//        //                            if (structuredResponse != null && structuredResponse.ContainsKey("redirect"))
//        //                            {
//        //                            if (structuredResponse.TryGetValue("redirect", out var redirectValue) &&
//        //                                redirectValue is JsonElement jsonElement &&
//        //                                jsonElement.ValueKind == JsonValueKind.True) // Checks explicitly if it's a JSON boolean
//        //                            {
//        //                                // ✅ Extract `teamId` safely
//        //                                if (structuredResponse.TryGetValue("teamId", out var teamIdValue) &&
//        //                                    teamIdValue is JsonElement teamIdElement &&
//        //                                    teamIdElement.ValueKind == JsonValueKind.String)
//        //                                {
//        //                                    targetTeamId = teamIdElement.GetString(); // ✅ Extract the string value
//        //                                }
//        //                            }

//        //                        }
//        //                    }
//        //                        catch (JsonException ex)
//        //                        {
//        //                            Console.WriteLine($"⚠️ JSON Parsing Error: {ex.Message}");
//        //                            // Ignore JSON errors and treat as normal response
//        //                        }
//        //                    //}
//        //                }
//        //            }
//        //            catch (Exception ex)
//        //            {
//        //                Console.WriteLine($"An error occurred: {ex.Message}");
//        //            }


//        //            return (true, gbtContentResponse, targetTeamId);
//        //        }



//        //public async Task<(bool answered, string response)> HandleUserQueryAsync(string userQuery, int companyId)
//        //{
//        //    // 1️⃣ Retrieve relevant documents from Azure Cognitive Search
//        //    var searchResults = await _searchService.QueryIndexAsync(userQuery, companyId);

//        //    if (!searchResults.Any())
//        //        return (false, "No relevant information found. Escalating to human agent.");

//        //    var chatCompletionsOptions = new ChatCompletionOptions()
//        //    {
//        //        Temperature = 0.7f,
//        //        MaxOutputTokenCount = 500, // Use MaxOutputTokenCount instead of MaxTokens
//        //        FrequencyPenalty = 0,
//        //        PresencePenalty = 0
//        //    };

//        //    // ✅ Pass messages directly in the API call


//        //    var messages = new List<ChatMessage>
//        //    {
//        //        new SystemChatMessage("You are an AI assistant that provides accurate responses based strictly on indexed company data. Always refer to the provided information and do not speculate."),
//        //        new UserChatMessage($"User Query: {userQuery}\n\n" +
//        //                            "Relevant Information:\n" +
//        //                            $"{FormatSearchResults(searchResults)}\n\n" +
//        //                            "Provide an answer using only the provided context.")
//        //    };


//        //    // ✅ Use chat client with correct method

//        //    ChatClient chatClient = _openAiClient.GetChatClient(_gptDeploymentName);
//        //    //var response = await chatClient.CompleteChatAsync(messages, chatCompletionsOptions);
//        //    var gbtContentResponse = "";
//        //    try
//        //    {
//        //        // Create the chat completion request
//        //        ChatCompletion completion = await chatClient.CompleteChatAsync(messages, chatCompletionsOptions);

//        //        // Print the response
//        //        if (completion.Content != null && completion.Content.Count > 0)
//        //        {
//        //            Console.WriteLine($"{completion.Content[0].Kind}: {completion.Content[0].Text}");
//        //            gbtContentResponse = completion.Content[0].Text;
//        //        }
//        //        else
//        //        {
//        //            Console.WriteLine("No response received.");
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Console.WriteLine($"An error occurred: {ex.Message}");
//        //    }

//        //    return (true, gbtContentResponse);
//        //}



//        private string FormatSearchResults(List<SearchResult> searchResults)
//        {
//            var formattedResults = new StringBuilder();

//            foreach (var result in searchResults)
//            {
//                formattedResults.AppendLine($"📌 **Source: {result.FileDescription}**");

//                try
//                {
//                    if (!string.IsNullOrEmpty(result.Content))
//                    {
//                        // 🔹 Extract JSON part using `content:` as reference
//                        int jsonStartIndex = result.Content.IndexOf("content:");
//                        if (jsonStartIndex != -1)
//                        {
//                            string jsonContent = result.Content.Substring(jsonStartIndex + 8).Trim();

//                            // 🔹 Ensure we correctly isolate the JSON block
//                            int jsonArrayStart = jsonContent.IndexOf("[");
//                            int jsonArrayEnd = jsonContent.LastIndexOf("]");
//                            int jsonObjectStart = jsonContent.IndexOf("{");
//                            int jsonObjectEnd = jsonContent.LastIndexOf("}");

//                            if (jsonArrayStart != -1 && jsonArrayEnd != -1)
//                            {
//                                // Extract JSON Array (`[]`)
//                                jsonContent = jsonContent.Substring(jsonArrayStart, jsonArrayEnd - jsonArrayStart + 1);

//                                var contentArray = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(jsonContent);

//                                if (contentArray != null)
//                                {
//                                    foreach (var item in contentArray)
//                                    {
//                                        formattedResults.AppendLine("🔹 Contact Entry:");
//                                        foreach (var (key, value) in item)
//                                        {
//                                            formattedResults.AppendLine($"  - **{key}**: {ConvertJsonElementToString(value)}");
//                                        }
//                                    }
//                                }
//                            }
//                            else if (jsonObjectStart != -1 && jsonObjectEnd != -1)
//                            {
//                                // Extract JSON Object (`{}`)
//                                jsonContent = jsonContent.Substring(jsonObjectStart, jsonObjectEnd - jsonObjectStart + 1);

//                                var contentJson = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

//                                if (contentJson != null)
//                                {
//                                    foreach (var (key, value) in contentJson)
//                                    {
//                                        formattedResults.AppendLine($"- **{key}**: {ConvertJsonElementToString(value)}");
//                                    }
//                                }
//                            }
//                            else
//                            {
//                                formattedResults.AppendLine("⚠️ Could not extract valid JSON content.");
//                            }
//                        }
//                        else
//                        {
//                            formattedResults.AppendLine("⚠️ No valid JSON found in content.");
//                        }
//                    }
//                    else
//                    {
//                        formattedResults.AppendLine("⚠️ No content available.");
//                    }
//                }
//                catch (JsonException ex)
//                {
//                    formattedResults.AppendLine($"⚠️ Error parsing content: {ex.Message}");
//                }

//                formattedResults.AppendLine("------\n"); // Separator
//            }

//            return formattedResults.ToString();
//        }



//        private string ConvertJsonElementToString(JsonElement element)
//        {
//            return element.ValueKind switch
//            {
//                JsonValueKind.String => element.GetString() ?? "",
//                JsonValueKind.Number => element.ToString(), // Converts numbers safely
//                JsonValueKind.True => "true",
//                JsonValueKind.False => "false",
//                JsonValueKind.Null => "null",
//                _ => element.ToString() // Fallback for unexpected cases
//            };
//        }




//    }
//}

