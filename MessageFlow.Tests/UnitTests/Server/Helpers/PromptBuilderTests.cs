using MessageFlow.Server.MediatR.Chat.AiBotProcessing.Helpers;
using MessageFlow.Shared.DTOs;
using System.Reflection;

namespace MessageFlow.Tests.UnitTests.Server.Helpers
{
    public class PromptBuilderTests
    {
        [Fact]
        public void WithResults_ValidInput_ReturnsFormattedPrompt()
        {
            var companyName = "TestComp";
            var history = "User: Hi\nAI: Hello!";
            var results = new List<SearchResultDTO>
            {
                new()
                {
                    FileDescription = "Support Data",
                    Content = """
                    content: [
                        {
                            "teamId": "1",
                            "teamName": "Support",
                            "email": "support@example.com"
                        }
                    ]
                    """
                }
            };

            var prompt = PromptBuilder.WithResults(history, results, companyName);

            Assert.Contains("You are an AI assistant", prompt);
            Assert.Contains("Support Data", prompt);
            Assert.Contains("teamName", prompt);
            Assert.Contains("support@example.com", prompt);
        }

        [Fact]
        public void NoResults_ValidInput_ReturnsPromptWithFallback()
        {
            var companyName = "TestComp";
            var history = "User: Help!";
            var fallbackTeams = new List<SearchResultDTO>
            {
                new()
                {
                    FileDescription = "Fallback Team",
                    Content = """
                    content: [
                        {
                            "teamId": "2",
                            "teamName": "Fallback",
                            "email": "fallback@example.com"
                        }
                    ]
                    """
                }
            };

            var prompt = PromptBuilder.NoResults(history, fallbackTeams, companyName);

            Assert.Contains("No indexed company data", prompt);
            Assert.Contains("Fallback Team", prompt);
            Assert.Contains("Fallback", prompt);
        }

        [Fact]
        public void WithResults_InvalidJson_ShowsErrorMessage()
        {
            var companyName = "TestComp";
            var results = new List<SearchResultDTO>
            {
                new()
                {
                    FileDescription = "Bad Data",
                    Content = "content: [ { invalid } ]"
                }
            };

            var prompt = PromptBuilder.WithResults("Chat history", results, companyName);

            Assert.Contains("Bad Data", prompt);
            Assert.Contains("Failed to parse", prompt);
        }

        [Fact]
        public void ExtractJsonBlock_ValidRange_ReturnsBlock()
        {
            string raw = "content: [{ \"a\": 1 }, { \"b\": 2 }]";
            string json = raw.Substring(8).Trim();

            var method = typeof(PromptBuilder).GetMethod("ExtractJsonBlock", BindingFlags.NonPublic | BindingFlags.Static);
            var result = (string)method!.Invoke(null, new object[] { json })!;

            Assert.StartsWith("[", result);
            Assert.EndsWith("]", result);
        }
    }
}
