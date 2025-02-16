namespace MessageFlow.Components.AzureServices.DTO
{
    public class SearchResult
    {
        public string Id { get; set; }
        public string FileDescription { get; set; }
        public string Content { get; set; }
        public string ProcessedAt { get; set; }
        public double? Score { get; set; }
    }
}
