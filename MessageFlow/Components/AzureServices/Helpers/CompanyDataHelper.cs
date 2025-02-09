using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using System.Globalization;
using System.Text.Json;

namespace MessageFlow.Components.Accounts.Helpers
{
    public static class CompanyDataHelper
    {
        /// <summary>
        /// Parses a CSV file stream into a structured dictionary.
        /// </summary>
        public static List<Dictionary<string, string>> ParseCsv(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            var records = new List<Dictionary<string, string>>();

            foreach (var record in csv.GetRecords<dynamic>())
            {
                var rowDict = new Dictionary<string, string>();
                foreach (var property in ((IDictionary<string, object>)record))
                {
                    rowDict[property.Key] = property.Value?.ToString() ?? string.Empty;
                }
                records.Add(rowDict);
            }
            return records;
        }

        /// <summary>
        /// Parses an Excel file stream into a structured dictionary.
        /// </summary>
        public static List<Dictionary<string, string>> ParseExcel(Stream fileStream)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            var data = new List<Dictionary<string, string>>();

            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0]; // First sheet
            var rowCount = worksheet.Dimension.Rows;
            var colCount = worksheet.Dimension.Columns;

            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                headers.Add(worksheet.Cells[1, col].Text.Trim());
            }

            for (int row = 2; row <= rowCount; row++)
            {
                var rowDict = new Dictionary<string, string>();
                for (int col = 1; col <= colCount; col++)
                {
                    rowDict[headers[col - 1]] = worksheet.Cells[row, col].Text.Trim();
                }
                data.Add(rowDict);
            }
            return data;
        }

        /// <summary>
        /// Processes extracted text from a document into structured JSON for Azure Search.
        /// </summary>
        public static string ProcessMetadataForAzureSearch(string extractedText)
        {
            try
            {
                var knowledgeBase = new List<object>();

                if (DetectFAQPattern(extractedText))
                {
                    var faqs = ExtractFAQs(extractedText);
                    knowledgeBase.AddRange(faqs.Select(faq => new
                    {
                        id = $"faq-{Guid.NewGuid()}",
                        type = "FAQ",
                        question = faq.Key,
                        answer = faq.Value
                    }));
                }
                else
                {
                    knowledgeBase.Add(new
                    {
                        id = $"doc-{Guid.NewGuid()}",
                        type = "TextDocument",
                        content = extractedText
                    });
                }

                return JsonSerializer.Serialize(knowledgeBase, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Error processing metadata: {ex.Message}");
                return string.Empty;
            }
        }

        private static bool DetectFAQPattern(string text)
        {
            return text.Contains("\nQ") && text.Contains("\nA:");
        }

        private static List<KeyValuePair<string, string>> ExtractFAQs(string text)
        {
            var faqs = new List<KeyValuePair<string, string>>();
            var questions = text.Split("\nQ", StringSplitOptions.RemoveEmptyEntries);

            foreach (var question in questions)
            {
                var parts = question.Split("A:", 2);
                if (parts.Length == 2)
                {
                    faqs.Add(new KeyValuePair<string, string>("Q" + parts[0].Trim(), parts[1].Trim()));
                }
            }
            return faqs;
        }
    }
}
