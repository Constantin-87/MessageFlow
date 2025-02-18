namespace MessageFlow.AzureServices.Helpers
{
    public class SearchIndexHelper
    {
        public static string GetIndexName(int companyId)
        {
            return $"company_{companyId}_index";
        }
    }
}
