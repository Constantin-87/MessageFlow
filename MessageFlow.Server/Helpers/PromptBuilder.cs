using System.Text;
using System.Text.Json;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.Chat.AiBotProcessing.Helpers
{
    public static class PromptBuilder
    {
        public static string WithResults(string history, List<SearchResultDTO> results, string companyName)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                $"You are an AI assistant for {companyName}, trained to assist users using only indexed internal company data.\n" +
                "Avoid speculation, and do not invent information beyond what is provided.\n\n" +
                "Guidelines:\n" +
                "- Only introduce yourself if this is the first user message.\n" +
                "- Use the chat history to maintain continuity and avoid repeating yourself.\n" +
                "- Be helpful, conversational, and funny-sarcastic if needed.\n\n" +
                "Example responses:\n" +
                $"- \"You're asking about {companyName} — here's what I found:\"\n" +
                $"- \"Hello! I'm {companyName}'s virtual assistant, here to help. What would you like to know?\"\n\n" +
                "**Chat History:**");

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

        public static string NoResults(string history, List<SearchResultDTO> fallbackTeams, string companyName)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                $"You are an AI assistant for {companyName}. No indexed company data was found that directly answers the user's question.\n\n" +
                "Guidelines:\n" +
                "- Do not say \"I have no data.\"\n" +
                "- Only introduce yourself if this is the first user message.\n" +
                "- Instead, ask the user to clarify or offer to redirect them to a support team.\n" +
                "- Use the chat history to infer possible intent.\n" +
                "- Be helpful, conversational, and funny-sarcastic if needed.\n\n" +
                "Example responses:\n" +
                "- \"I'm not sure about that topic, but I can connect you with our Info team if you'd like?\"\n" +
                "- \"It seems I don't have specific data here. Would you like to be redirected to Sales or Support?\"\n\n" +
                "**Chat History:**");

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

        private static string ConvertJsonElementToString(JsonElement e)
        {
            return e.ValueKind switch
            {
                JsonValueKind.String => e.GetString() ?? "",
                JsonValueKind.Number => e.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                _ => e.ToString()
            };
        }
    }
}