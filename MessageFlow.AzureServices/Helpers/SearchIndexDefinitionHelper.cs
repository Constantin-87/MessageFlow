using Azure.Search.Documents.Indexes.Models;

namespace MessageFlow.AzureServices.Helpers
{
    public static class SearchIndexDefinitionHelper
    {
        /// <summary>
        /// Generates the field definitions for an Azure Search index based on structured data.
        /// </summary>
        public static List<SearchField> GenerateIndexFields(Dictionary<string, object> structuredFields)
        {
            var fields = new List<SearchField>
            {
                new SearchField("id", SearchFieldDataType.String) { IsKey = true }
            };

            foreach (var field in structuredFields)
            {
                var searchField = CreateSearchField(field.Key, field.Value);
                if (searchField != null)
                {
                    fields.Add(searchField);
                }
            }

            return fields;
        }

        private static SearchField CreateSearchField(string fieldName, object fieldValue)
        {
            fieldName = NormalizeFieldName(fieldName);

            if (fieldValue is Dictionary<string, object> nestedStructure)
            {
                var field = new SearchField(fieldName, SearchFieldDataType.Complex);
                foreach (var kv in nestedStructure)
                {
                    field.Fields.Add(CreateSearchField(kv.Key, kv.Value));
                }
                return field;
            }
            else if (fieldValue is List<Dictionary<string, object>> complexList && complexList.Count > 0)
            {
                var field = new SearchField(fieldName, SearchFieldDataType.Collection(SearchFieldDataType.Complex));
                var allKeys = complexList.SelectMany(dict => dict.Keys).Distinct().ToList();

                foreach (var key in allKeys)
                {
                    var sampleValue = complexList.FirstOrDefault(dict => dict.ContainsKey(key))?[key];
                    if (sampleValue != null)
                    {
                        field.Fields.Add(CreateSearchField(key, sampleValue));
                    }
                }

                return field;
            }
            else if (fieldValue is List<object> primitiveList)
            {
                return new SearchField(fieldName, SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsSearchable = true };
            }
            else
            {
                return new SearchField(fieldName, DetermineSearchFieldType(fieldValue)) { IsSearchable = true };
            }
        }

        private static string NormalizeFieldName(string fieldName)
        {
            fieldName = fieldName.Replace(" ", "_");
            fieldName = System.Text.RegularExpressions.Regex.Replace(fieldName, @"[^a-zA-Z0-9_]", "");
            return char.IsLetter(fieldName[0]) ? fieldName.ToLowerInvariant() : "f_" + fieldName.ToLowerInvariant();
        }

        private static SearchFieldDataType DetermineSearchFieldType(object value)
        {
            return value switch
            {
                int or long => SearchFieldDataType.Int64,
                float or double => SearchFieldDataType.Double,
                bool => SearchFieldDataType.Boolean,
                DateTime => SearchFieldDataType.DateTimeOffset,
                _ => SearchFieldDataType.String
            };
        }
    }
}