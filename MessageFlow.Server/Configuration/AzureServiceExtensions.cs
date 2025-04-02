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

            if (string.IsNullOrEmpty(searchServiceEndpoint) || string.IsNullOrEmpty(searchServiceApiKey))
                throw new InvalidOperationException("Azure Search configuration is missing.");

            services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();

            services.AddScoped<IAzureSearchService>(provider =>
            {
                var blobService = provider.GetRequiredService<IAzureBlobStorageService>();
                return new AzureSearchService(searchServiceEndpoint, searchServiceApiKey, blobService);
            });

            services.AddScoped<AzureSearchQueryService>();

            return services;
        }
    }

}
