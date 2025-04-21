using MessageFlow.AzureServices.Helpers;

namespace MessageFlow.Tests.Tests.AzureServices.Helpers;

public class JsonStructureHelperTests
{
    [Fact]
    public void ExtractJsonStructure_HandlesFlatJson()
    {
        string json = """{ "Name": "Alice", "Age": 30 }""";

        var structure = JsonStructureHelper.ExtractJsonStructure(json);

        Assert.Equal("Alice", structure["name"]);
        Assert.Equal("30", structure["age"]);
    }

    [Fact]
    public void ExtractJsonStructure_HandlesNestedObject()
    {
        string json = """
        {
            "User": {
                "First Name": "Bob",
                "Email": "bob@example.com"
            }
        }
        """;

        var structure = JsonStructureHelper.ExtractJsonStructure(json);

        Assert.True(structure.ContainsKey("user"));
        var nested = (Dictionary<string, object>)structure["user"];
        Assert.Equal("bob@example.com", nested["email"]);
        Assert.Equal("Bob", nested["first_name"]);
    }

    [Fact]
    public void ExtractJsonStructure_HandlesArrayOfPrimitives()
    {
        string json = """{ "tags": ["one", "two", "three"] }""";

        var structure = JsonStructureHelper.ExtractJsonStructure(json);

        var tags = Assert.IsType<List<string>>(structure["tags"]);
        Assert.Contains("one", tags);
        Assert.Equal(3, tags.Count);
    }

    [Fact]
    public void ExtractJsonStructure_HandlesArrayOfObjects()
    {
        string json = """
        {
            "Items": [
                { "Id": 1, "Value": "A" },
                { "Id": 2, "Value": "B" }
            ]
        }
        """;

        var structure = JsonStructureHelper.ExtractJsonStructure(json);

        var items = Assert.IsType<List<Dictionary<string, object>>>(structure["items"]);
        Assert.Equal("1", items[0]["id"]);
        Assert.Equal("B", items[1]["value"]);
    }

    [Fact]
    public void ExtractJsonStructure_HandlesEmptyArray()
    {
        string json = """{ "emptyList": [] }""";

        var structure = JsonStructureHelper.ExtractJsonStructure(json);

        var list = Assert.IsType<List<object>>(structure["emptylist"]);
        Assert.Empty(list);
    }

    [Fact]
    public void NormalizeFieldName_RemovesSpecialChars()
    {
        string json = """{ "Field@Name With Spaces!": "test" }""";

        var structure = JsonStructureHelper.ExtractJsonStructure(json);

        Assert.True(structure.ContainsKey("fieldname_with_spaces"));
        Assert.Equal("test", structure["fieldname_with_spaces"]);
    }
}