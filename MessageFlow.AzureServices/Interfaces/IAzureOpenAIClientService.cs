using Azure.AI.OpenAI;

namespace MessageFlow.AzureServices.Interfaces
{
    public interface IAzureOpenAIClientService
    {
        AzureOpenAIClient GetAzureClient();
    }
}