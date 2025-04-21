using System.Text.Json;

namespace MessageFlow.AzureServices.Helpers
{
    public static class JsonStructureHelper
    {
        public static Dictionary<string, object> ExtractJsonStructure(string jsonContent)
        {
            var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;
            var structure = new Dictionary<string, object>();

            ExtractFieldsRecursively(root, structure);
            return structure;
        }

        private static void ExtractFieldsRecursively(JsonElement element, Dictionary<string, object> structure)
        {
            foreach (var property in element.EnumerateObject())
            {
                string fieldName = NormalizeFieldName(property.Name);

                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.Object:
                        var nestedStructure = new Dictionary<string, object>();
                        ExtractFieldsRecursively(property.Value, nestedStructure);
                        structure[fieldName] = nestedStructure;
                        break;

                    case JsonValueKind.Array:
                        if (property.Value.GetArrayLength() > 0)
                        {
                            var firstElement = property.Value.EnumerateArray().First();

                            if (firstElement.ValueKind == JsonValueKind.Object)
                            {
                                var arrayStructure = new List<Dictionary<string, object>>();
                                foreach (var item in property.Value.EnumerateArray())
                                {
                                    var itemStructure = new Dictionary<string, object>();
                                    ExtractFieldsRecursively(item, itemStructure);
                                    arrayStructure.Add(itemStructure);
                                }
                                structure[fieldName] = arrayStructure;
                            }
                            else
                            {
                                structure[fieldName] = property.Value.EnumerateArray().Select(v => v.ToString()).ToList();
                            }
                        }
                        else
                        {
                            structure[fieldName] = new List<object>();
                        }
                        break;

                    default:
                        structure[fieldName] = property.Value.ToString();
                        break;
                }
            }
        }

        private static string NormalizeFieldName(string fieldName)
        {
            fieldName = fieldName.Replace(" ", "_");
            fieldName = System.Text.RegularExpressions.Regex.Replace(fieldName, @"[^a-zA-Z0-9_]", "");
            return fieldName.ToLowerInvariant();
        }
    }
}