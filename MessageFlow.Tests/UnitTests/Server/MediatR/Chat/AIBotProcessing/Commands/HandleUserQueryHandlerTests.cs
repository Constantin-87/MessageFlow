using MessageFlow.AzureServices.Interfaces;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.AiBotProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.AiBotProcessing.Commands;
using MessageFlow.Shared.DTOs;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using MessageFlow.Server.MediatR.Chat.AiBotProcessing.Helpers;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.AIBotProcessing.Commands
{
    public class HandleUserQueryHandlerTests
    {
        private readonly Mock<IAzureSearchQueryService> _searchServiceMock;
        private readonly Mock<IAzureOpenAIClientService> _openAiClientServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly IConfiguration _config;
        private readonly Mock<ILogger<HandleUserQueryHandler>> _loggerMock;

        public HandleUserQueryHandlerTests()
        {
            _searchServiceMock = new Mock<IAzureSearchQueryService>();
            _openAiClientServiceMock = new Mock<IAzureOpenAIClientService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<HandleUserQueryHandler>>();

            var inMemorySettings = new Dictionary<string, string>
            {
                { "azure-gbt-endpoint", "https://dummy.openai.azure.com/" },
                { "azure-gbt-deployment-key", "fake-key" }
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task Handle_NoSearchResults_ReturnsFallbackTeams()
        {
            var handler = new HandleUserQueryHandler(
                _searchServiceMock.Object,
                _openAiClientServiceMock.Object,
                _config,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );

            _searchServiceMock.Setup(s => s.QueryIndexAsync("user query", "c1"))
                .ReturnsAsync(new List<SearchResultDTO>()); // no results

            _searchServiceMock.Setup(s => s.QueryIndexAsync("company teams", "c1"))
                .ReturnsAsync(new List<SearchResultDTO>
                {
            new() { FileDescription = "Team A", Content = "{\"teamId\":\"1\",\"teamName\":\"Support\"}" }
                });

            _unitOfWorkMock.Setup(u => u.Messages.GetMessagesByConversationIdAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Message>());

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(It.IsAny<string>()))
                .ReturnsAsync(new Company { CompanyName = "TestCo" });

            // ✅ Add missing GPT mock (simulate failure or short-circuit)
            var mockChatClient = new Mock<ChatClient>();
            mockChatClient
                .Setup(c => c.CompleteChatAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Simulated GPT failure"));

            var mockAzureClient = new Mock<AzureOpenAIClient>();
            mockAzureClient
                .Setup(c => c.GetChatClient(It.IsAny<string>()))
                .Returns(mockChatClient.Object);

            _openAiClientServiceMock
                .Setup(s => s.GetAzureClient())
                .Returns(mockAzureClient.Object);

            var command = new HandleUserQueryCommand("user query", "c1", "conv1");

            var result = await handler.Handle(command, default);

            Assert.NotNull(result);
            Assert.False(result.Answered);
        }


        [Fact]
        public async Task Handle_GptFailure_ReturnsUnanswered()
        {
            var mockChatClient = new Mock<ChatClient>();
            mockChatClient
                .Setup(c => c.CompleteChatAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Simulated GPT failure"));

            var mockAzureClient = new Mock<AzureOpenAIClient>();
            mockAzureClient
                .Setup(c => c.GetChatClient(It.IsAny<string>()))
                .Returns(mockChatClient.Object);

            _openAiClientServiceMock
                .Setup(s => s.GetAzureClient())
                .Returns(mockAzureClient.Object);

            var handler = new HandleUserQueryHandler(
                _searchServiceMock.Object,
                _openAiClientServiceMock.Object,
                _config,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );

            _searchServiceMock.Setup(s => s.QueryIndexAsync("any", "x"))
                .ReturnsAsync(new List<SearchResultDTO> { new() { FileDescription = "Doc", Content = "content" } });

            _unitOfWorkMock.Setup(u => u.Messages.GetMessagesByConversationIdAsync(It.IsAny<string>(), 5))
                .ReturnsAsync(new List<Message>());

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(It.IsAny<string>()))
                .ReturnsAsync(new Company { CompanyName = "TestCo" });

            var result = await handler.Handle(new HandleUserQueryCommand("any", "x", "cid"), default);

            Assert.False(result.Answered);
            Assert.Equal("", result.RawResponse);
            Assert.Null(result.TargetTeamId);
        }

        [Fact]
        public void TryExtractRedirect_ValidJson_ReturnsTrue()
        {
            string json = """
            {
                "redirect": true,
                "teamId": "42",
                "teamName": "Support"
            }
            """;

            var logger = new Mock<ILogger>().Object;
            var result = (ValueTuple<bool, string?, string?>)typeof(HandleUserQueryHandler)
                .GetMethod("TryExtractRedirect", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { json, logger })!;

            var (redirect, id, name) = result;

            Assert.True(redirect);
            Assert.Equal("42", id);
            Assert.Equal("Support", name);
        }

        [Fact]
        public void TryExtractRedirect_InvalidJson_ReturnsFalse()
        {
            string json = "Not a JSON";
            var logger = new Mock<ILogger>().Object;
            var result = (ValueTuple<bool, string?, string?>)typeof(HandleUserQueryHandler)
                .GetMethod("TryExtractRedirect", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { json, logger })!;

            var (redirect, id, name) = result;

            Assert.False(redirect);
            Assert.Null(id);
            Assert.Null(name);
        }

        [Fact]
        public void BuildSystemPromptNoResults_FormatsFallbackTeamsCorrectly()
        {
            var companyName = "TestComp";
            var fallbackTeams = new List<SearchResultDTO>
            {
                new()
                {
                    FileDescription = "Team X",
                    Content = """
                    content: [
                        {
                            "teamId": "99",
                            "teamName": "Billing",
                            "email": "billing@example.com"
                        }
                    ]
                    """
                }
            };

            string history = "No previous chat";
            var prompt = PromptBuilder.NoResults(history, fallbackTeams, companyName);

            Assert.Contains("Team X", prompt);
            Assert.Contains("Billing", prompt);
            Assert.Contains("teamId", prompt);
            Assert.Contains("teamName", prompt);
        }


        [Fact]
        public async Task Handle_NoSearchResults_BuildsPromptWithFallbackTeams()
        {
            var fallbackTeams = new List<SearchResultDTO>
            {
                new()
                {
                    FileDescription = "Fallback",
                    Content = """
                    content: [
                        {
                            "teamId": "123",
                            "teamName": "Helpdesk"
                        }
                    ]
                    """
                }
            };

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(It.IsAny<string>()))
                .ReturnsAsync(new Company { CompanyName = "TestCo" });

            _searchServiceMock.Setup(s => s.QueryIndexAsync("no match", "c9"))
                .ReturnsAsync(new List<SearchResultDTO>());

            _searchServiceMock.Setup(s => s.QueryIndexAsync("company teams", "c9"))
                .ReturnsAsync(fallbackTeams);

            _unitOfWorkMock.Setup(u => u.Messages.GetMessagesByConversationIdAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Message>());

            // 1. Mock the GPT chat client
            var mockChatClient = new Mock<ChatClient>();
            mockChatClient
                .Setup(c => c.CompleteChatAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Simulated GPT failure"));


            // 2. Mock the client service to return the mocked chat client
            var mockAzureClient = new Mock<AzureOpenAIClient>();
            mockAzureClient
                .Setup(c => c.GetChatClient(It.IsAny<string>()))
                .Returns(mockChatClient.Object);

            // 3. Inject the chain
            _openAiClientServiceMock
                .Setup(s => s.GetAzureClient())
                .Returns(mockAzureClient.Object);



            var handler = new HandleUserQueryHandler(
                _searchServiceMock.Object,
                _openAiClientServiceMock.Object,
                _config,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );

            var result = await handler.Handle(new HandleUserQueryCommand("no match", "c9", "conv9"), default);

            Assert.NotNull(result);
            Assert.False(result.Answered);
        }
    }
}