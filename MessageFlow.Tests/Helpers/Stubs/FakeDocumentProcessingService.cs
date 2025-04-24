using MessageFlow.AzureServices.Interfaces;

namespace MessageFlow.Tests.Helpers.Stubs
{
    public class FakeDocumentProcessingService : IDocumentProcessingService
    {
        public Task<string> ExtractTextFromDocumentAsync(Stream fileStream, string contentType)
        {
            return Task.FromResult("Fake extracted text content");
        }
    }
}