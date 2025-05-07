using MessageFlow.AzureServices.Interfaces;
using MessageFlow.AzureServices.Services;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace MessageFlow.Tests.UnitTests.AzureServices
{
    public class AzureOpenAIClientServiceTests
    {
        private readonly IAzureOpenAIClientService _service;

        public AzureOpenAIClientServiceTests()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "azure-gbt-endpoint", "https://example.openai.azure.com/" },
                    { "azure-gbt-deployment-key", "dummy-key" }
                })
                .Build();

            _service = new AzureOpenAIClientService(config);
        }

        [Fact]
        public void GetAzureClient_ReturnsNonNullClient()
        {
            var client = _service.GetAzureClient();
            Assert.NotNull(client);
        }

        [Fact]
        public void GetChatClient_FromAzureClient_ReturnsNonNullChatClient()
        {
            var deployment = "test-model";
            var client = _service.GetAzureClient();
            var chatClient = client.GetChatClient(deployment);

            Assert.NotNull(chatClient);
            Assert.IsType<ChatClient>(chatClient);
        }
    }
}
