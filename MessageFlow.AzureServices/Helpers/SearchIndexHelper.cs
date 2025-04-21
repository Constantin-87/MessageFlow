namespace MessageFlow.AzureServices.Helpers
{
    public class SearchIndexHelper
    {
        public static string GetIndexName(string companyId)
        {
            return $"company_{companyId}_index";
        }
    }
}