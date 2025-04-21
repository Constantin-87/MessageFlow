using MessageFlow.AzureServices.Helpers;
using Xunit;

namespace MessageFlow.Tests.Tests.AzureServices.Helpers;

public class SearchIndexHelperTests
{
    [Theory]
    [InlineData("abc", "company_abc_index")]
    [InlineData("123", "company_123_index")]
    [InlineData("Company-X", "company_Company-X_index")]
    public void GetIndexName_ReturnsExpectedFormat(string input, string expected)
    {
        var result = SearchIndexHelper.GetIndexName(input);
        Assert.Equal(expected, result);
    }
}