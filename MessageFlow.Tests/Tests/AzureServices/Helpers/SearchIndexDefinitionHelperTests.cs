using Azure.Search.Documents.Indexes.Models;
using MessageFlow.AzureServices.Helpers;

namespace MessageFlow.Tests.Tests.AzureServices.Helpers;

public class SearchIndexDefinitionHelperTests
{
    [Fact]
    public void GenerateIndexFields_IncludesIdField()
    {
        var fields = SearchIndexDefinitionHelper.GenerateIndexFields(new());

        var idField = fields.FirstOrDefault(f => f.Name == "id");
        Assert.NotNull(idField);
        Assert.True(idField.IsKey);
    }

    [Fact]
    public void GenerateIndexFields_AddsPrimitiveFields()
    {
        var data = new Dictionary<string, object>
        {
            { "Title", "Sample" },
            { "Views", 123 },
            { "Active", true }
        };

        var fields = SearchIndexDefinitionHelper.GenerateIndexFields(data);

        Assert.Contains(fields, f => f.Name == "title" && f.Type == SearchFieldDataType.String);
        Assert.Contains(fields, f => f.Name == "views" && f.Type == SearchFieldDataType.Int64);
        Assert.Contains(fields, f => f.Name == "active" && f.Type == SearchFieldDataType.Boolean);
    }

    [Fact]
    public void GenerateIndexFields_AddsNestedObject()
    {
        var data = new Dictionary<string, object>
        {
            { "Author", new Dictionary<string, object> { { "Name", "Alice" }, { "Age", 30 } } }
        };

        var fields = SearchIndexDefinitionHelper.GenerateIndexFields(data);
        var authorField = fields.First(f => f.Name == "author");

        Assert.Equal(SearchFieldDataType.Complex, authorField.Type);
        Assert.Contains(authorField.Fields, f => f.Name == "name");
        Assert.Contains(authorField.Fields, f => f.Name == "age");
    }

    [Fact]
    public void GenerateIndexFields_AddsListOfObjects()
    {
        var data = new Dictionary<string, object>
        {
            {
                "Items", new List<Dictionary<string, object>>
                {
                    new() { { "Id", 1 }, { "Name", "A" } },
                    new() { { "Id", 2 }, { "Name", "B" } }
                }
            }
        };

        var fields = SearchIndexDefinitionHelper.GenerateIndexFields(data);
        var itemsField = fields.First(f => f.Name == "items");

        Assert.Equal(SearchFieldDataType.Collection(SearchFieldDataType.Complex), itemsField.Type);
        Assert.Contains(itemsField.Fields, f => f.Name == "id");
        Assert.Contains(itemsField.Fields, f => f.Name == "name");
    }

    [Fact]
    public void GenerateIndexFields_AddsListOfPrimitives()
    {
        var data = new Dictionary<string, object>
        {
            { "Tags", new List<object> { "one", "two" } }
        };

        var fields = SearchIndexDefinitionHelper.GenerateIndexFields(data);
        var tagField = fields.First(f => f.Name == "tags");

        Assert.Equal(SearchFieldDataType.Collection(SearchFieldDataType.String), tagField.Type);
        Assert.True(tagField.IsSearchable);
    }

    [Fact]
    public void NormalizeFieldName_HandlesInvalidCharacters()
    {
        var data = new Dictionary<string, object>
        {
            { " 123-@!Field ", "value" }
        };

        var fields = SearchIndexDefinitionHelper.GenerateIndexFields(data);
        Assert.Contains(fields, f => f.Name == "f__123field_");
    }
    public static IEnumerable<object[]> DetermineFieldTypeData =>
    [
        [42, SearchFieldDataType.Int64],
        [42.5, SearchFieldDataType.Double],
        [true, SearchFieldDataType.Boolean],
        ["str", SearchFieldDataType.String],
        [DateTime.UtcNow, SearchFieldDataType.DateTimeOffset]
    ];

    [Theory]
    [MemberData(nameof(DetermineFieldTypeData))]
    public void DetermineSearchFieldType_MapsTypesCorrectly(object value, SearchFieldDataType expected)
    {
        var method = typeof(SearchIndexDefinitionHelper)
            .GetMethod("DetermineSearchFieldType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var result = (SearchFieldDataType)method.Invoke(null, [value])!;
        Assert.Equal(expected, result);
    }
}