namespace MessageFlow.AzureServices.Interfaces
{
    public interface IDocumentProcessingService
    {
        Task<string> ExtractTextFromDocumentAsync(Stream fileStream, string contentType);
    }
}