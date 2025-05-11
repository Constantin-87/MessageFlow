using Azure;
using Azure.AI.OpenAI;
using MessageFlow.AzureServices.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MessageFlow.AzureServices.Services
{
    public class AzureOpenAIClientService : IAzureOpenAIClientService
    {
        private readonly AzureOpenAIClient _azureClient;

        public AzureOpenAIClientService(IConfiguration config)
        {
            var endpoint = config["azure-gbt-endpoint"];
            var key = config["azure-gbt-deployment-key"];
            _azureClient = new AzureOpenAIClient(new Uri(endpoint!), new AzureKeyCredential(key!));
        }

        public AzureOpenAIClient GetAzureClient() => _azureClient;
    }
}