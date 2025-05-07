using Azure.Storage.Blobs;
using MessageFlow.AzureServices.Helpers.Interfaces;
using MessageFlow.AzureServices.Helpers;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.AzureServices.Services;

namespace MessageFlow.Server.Configuration
{
    public static class AzureServiceExtensions
    {
        public static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration config)
        {
            var searchServiceEndpoint = config["azure-ai-search-url"];
            var searchServiceApiKey = config["azure-ai-search-key"];
            var BlobStorageConn = config["azure-storage-account-conn-string"];

            if (string.IsNullOrEmpty(searchServiceEndpoint) || string.IsNullOrEmpty(searchServiceApiKey) || string.IsNullOrEmpty(BlobStorageConn))
                throw new InvalidOperationException("Azure configuration is missing.");

            services.AddScoped(provider =>
            {
                return new BlobServiceClient(BlobStorageConn);
            });

            services.AddScoped<IBlobRagHelper, BlobRagHelper>();
            services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();

            services.AddScoped<IAzureSearchService>(provider =>
            {
                var blobService = provider.GetRequiredService<IAzureBlobStorageService>();
                var logger = provider.GetRequiredService<ILogger<AzureSearchService>>();
                return new AzureSearchService(searchServiceEndpoint, searchServiceApiKey, blobService, logger);
            });

            services.AddScoped<AzureSearchQueryService>();
            services.AddScoped<IAzureOpenAIClientService, AzureOpenAIClientService>();

            return services;
        }
    }
}