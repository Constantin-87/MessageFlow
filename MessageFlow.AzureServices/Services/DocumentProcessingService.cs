using Azure;
using Azure.AI.DocumentIntelligence;
using MessageFlow.AzureServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MessageFlow.AzureServices.Services
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly DocumentIntelligenceClient _client;
        private readonly ILogger<DocumentProcessingService> _logger;

        public DocumentProcessingService(IConfiguration configuration, ILogger<DocumentProcessingService> logger)
        {
            _logger = logger;

            // Fetch credentials from Azure Key Vault
            string? endpoint = configuration["azure-documentintelligence-endpoint"];
            string? apiKey = configuration["azure-documentintelligence-key"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Azure Document Intelligence API credentials are missing.");
            }

            _client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        /// <summary>
        /// Analyze a document from a given file stream.
        /// </summary>        
        public async Task<string> ExtractTextFromDocumentAsync(Stream fileStream, string contentType)
        {
            try
            {
                string modelId = "prebuilt-layout";

                // Process the file using Azure Document Intelligence
                var operation = await GetClient().AnalyzeDocumentAsync(WaitUntil.Completed, modelId, BinaryData.FromStream(fileStream));
                var result = operation.Value;

                string extractedText = ExtractTextFromResult(result);

                _logger.LogInformation("Document processed successfully.");
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document.");
                return string.Empty;
            }
        }
        protected internal virtual DocumentIntelligenceClient GetClient() => _client;
        protected internal virtual string ExtractTextFromResult(AnalyzeResult result)
        {
            return string.Join(" ", result.Content);
        }
    }
}