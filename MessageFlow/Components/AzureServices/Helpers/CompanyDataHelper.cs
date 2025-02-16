using CsvHelper;
using CsvHelper.Configuration;
using MessageFlow.Components.AzureServices;
using MessageFlow.Models;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MessageFlow.Components.Accounts.Helpers
{
    public static class CompanyDataHelper
    {

        /// <summary>
        /// Processes a list of uploaded files and extracts structured data into separate documents.
        /// </summary>
        public static async Task<(List<ProcessedPretrainData>, List<string>)> ProcessUploadedFilesAsync(
            List<PretrainDataFile> uploadedFiles,
            DocumentProcessingService documentProcessingService)
        {
            var processedFiles = new List<ProcessedPretrainData>();
            var jsonContents = new List<string>();

            foreach (var file in uploadedFiles)
            {
                using var memoryStream = new MemoryStream();
                await file.FileContent.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset position for reading


                string fileExtension = Path.GetExtension(file.FileName).ToLower();

                List<ProcessedPretrainData> generatedFiles = new();
                List<string> generatedJsonContents = new();
                switch (fileExtension)
                {
                    case ".csv":
                        (generatedFiles, generatedJsonContents) = ConvertCsvToPretrainFiles(file, memoryStream);
                        break;

                    case ".xlsx":
                        (generatedFiles, generatedJsonContents) = ConvertExcelToPretrainFiles(file, memoryStream);
                        break;

                    default:
                        string extractedText = await documentProcessingService.ExtractTextFromDocumentAsync(memoryStream, "application/octet-stream");
                        (generatedFiles, generatedJsonContents) = ConvertTextToPretrainFiles(file, extractedText);
                        break;
                }

                processedFiles.AddRange(generatedFiles);
                jsonContents.AddRange(generatedJsonContents);
            }

            return (processedFiles, jsonContents);
        }

        /// <summary>
        /// Converts a CSV file stream into individual structured JSON documents.
        /// </summary>
        public static (List<ProcessedPretrainData>, List<string>) ConvertCsvToPretrainFiles(PretrainDataFile originalFile, Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            var processedFiles = new List<ProcessedPretrainData>();
            var jsonContents = new List<string>();

            foreach (var record in csv.GetRecords<dynamic>())
            {
                var rowDict = (IDictionary<string, object>)record;

                // Convert the row into a structured JSON string
                var jsonContent = JsonSerializer.Serialize(rowDict, new JsonSerializerOptions { WriteIndented = true });

                // Add to JSON contents list (for blob storage)
                jsonContents.Add(jsonContent);

                processedFiles.Add(new ProcessedPretrainData
                {
                    FileDescription = originalFile.FileDescription,
                    FileUrl = "",  // Will be set after uploading to Azure Blob Storage
                    CompanyId = originalFile.CompanyId,  // Associate with the correct company
                    FileType = FileType.CsvFile,
                    ProcessedAt = DateTime.UtcNow
                });
            }

            return (processedFiles, jsonContents);
        }



        /// <summary>
        /// Converts an Excel file stream into individual structured JSON documents.
        /// </summary>
        public static (List<ProcessedPretrainData>, List<string>) ConvertExcelToPretrainFiles(PretrainDataFile originalFile, Stream fileStream)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            var processedFiles = new List<ProcessedPretrainData>();
            var jsonContents = new List<string>();
            using var package = new ExcelPackage(fileStream);

            // 🔹 Loop through all sheets in the workbook
            foreach (var worksheet in package.Workbook.Worksheets)
            {
                if (worksheet.Dimension == null) continue; // Skip empty sheets

                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                // Extract headers
                var headers = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    headers.Add(worksheet.Cells[1, col].Text.Trim());
                }

                // Process each row in the current sheet
                for (int row = 2; row <= rowCount; row++)
                {
                    var rowDict = new Dictionary<string, string>();

                    for (int col = 1; col <= colCount; col++)
                    {
                        rowDict[headers[col - 1]] = worksheet.Cells[row, col].Text.Trim();
                    }

                    // Serialize the row data into a structured JSON format
                    var jsonContent = JsonSerializer.Serialize(rowDict, new JsonSerializerOptions { WriteIndented = true });
                    // Add to JSON contents list (for blob storage)
                    jsonContents.Add(jsonContent);

                    processedFiles.Add(new ProcessedPretrainData
                    {
                        FileDescription = originalFile.FileDescription,
                        FileUrl = "", // This will be set after uploading to Azure Blob Storage
                        CompanyId = originalFile.CompanyId,
                        FileType = FileType.ExcelFile,
                        ProcessedAt = DateTime.UtcNow
                    });
                }
            }

            return (processedFiles, jsonContents);
        }


        /// <summary>
        /// Converts extracted text from a document into structured JSON documents.
        /// </summary>
        public static (List<ProcessedPretrainData>, List<string>) ConvertTextToPretrainFiles(PretrainDataFile originalFile, string extractedText)
        {
            var processedFiles = new List<ProcessedPretrainData>();
            var jsonContents = new List<string>();

            // 🔹 If no FAQ pattern is detected, store the text as a general document
            if (!DetectFAQPattern(extractedText))
            {
                var document = new
                {
                    type = "TextDocument",
                    content = extractedText
                };

                var jsonContent = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });

                // Add JSON content to list (for Blob Storage)
                jsonContents.Add(jsonContent);

                // Add metadata for DB storage
                processedFiles.Add(new ProcessedPretrainData
                {
                    FileDescription = originalFile.FileDescription,
                    FileUrl = "",  // To be updated after uploading to Azure Blob Storage
                    CompanyId = originalFile.CompanyId,
                    FileType = FileType.Other,
                    ProcessedAt = DateTime.UtcNow
                });

                return (processedFiles, jsonContents);
            }

            // 🔹 If FAQ pattern is detected, extract and store each FAQ separately
            var faqs = ExtractFAQs(extractedText);
            foreach (var faq in faqs)
            {
                var faqDocument = new
                {
                    question = faq.Key,
                    answer = faq.Value
                };

                var jsonContent = JsonSerializer.Serialize(faqDocument, new JsonSerializerOptions { WriteIndented = true });

                // Add JSON content to list (for Blob Storage)
                jsonContents.Add(jsonContent);

                // Add metadata for DB storage
                processedFiles.Add(new ProcessedPretrainData
                {
                    FileDescription = "FAQ List",
                    FileUrl = "",  // To be updated after uploading to Azure Blob Storage
                    CompanyId = originalFile.CompanyId,
                    FileType = FileType.FAQFile,
                    ProcessedAt = DateTime.UtcNow
                });
            }

            return (processedFiles, jsonContents);
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

        /// <summary>
        /// Generates structured metadata documents for a company.
        /// </summary>
        public static (List<ProcessedPretrainData>, List<string>) GenerateStructuredCompanyMetadata(Company company)
        {
            var processedFiles = new List<ProcessedPretrainData>();
            var jsonContents = new List<string>();

            // 🔹 1. General Company Information Document
            var generalInfo = new
            {
                company_name = company.CompanyName,
                company_description = company.Description,
                industry_type = company.IndustryType,
                company_website = company.WebsiteUrl
            };

            var generalInfoJson = JsonSerializer.Serialize(generalInfo, new JsonSerializerOptions { WriteIndented = true });

            jsonContents.Add(generalInfoJson);

            processedFiles.Add(new ProcessedPretrainData
            {
                FileDescription = "General information about the company, including industry type and website.",
                FileUrl = "",  // To be updated after uploading to Azure Blob Storage
                CompanyId = company.Id,
                FileType = FileType.CompanyDetails,
                ProcessedAt = DateTime.UtcNow
            });

            // 🔹 2. Company Emails Document
            var companyEmails = company.CompanyEmails.Select(e => new { e.EmailAddress, e.Description }).ToList();
            var companyEmailsJson = JsonSerializer.Serialize(companyEmails, new JsonSerializerOptions { WriteIndented = true });

            jsonContents.Add(companyEmailsJson);

            processedFiles.Add(new ProcessedPretrainData
            {
                FileDescription = "Company contact emails. Use this for support, inquiries, or reaching out to relevant departments.",
                FileUrl = "",  // To be updated after uploading to Azure Blob Storage
                CompanyId = company.Id,
                FileType = FileType.CompanyEmails,
                ProcessedAt = DateTime.UtcNow
            });

            // 🔹 3. Company Phone Numbers Document
            var companyPhones = company.CompanyPhoneNumbers.Select(p => new { p.PhoneNumber, p.Description }).ToList();
            var companyPhonesJson = JsonSerializer.Serialize(companyPhones, new JsonSerializerOptions { WriteIndented = true });

            jsonContents.Add(companyPhonesJson); // Store JSON for Blob Storage

            processedFiles.Add(new ProcessedPretrainData
            {
                FileDescription = "Company contact phone numbers. Use this for customer support, direct contact, and urgent inquiries.",
                FileUrl = "",  // To be updated after uploading to Azure Blob Storage
                CompanyId = company.Id,
                FileType = FileType.CompanyPhoneNumbers,
                ProcessedAt = DateTime.UtcNow
            });


            // 🔹 4. Company Teams Document
            var companyTeams = company.Teams.Select(t => new { t.Id, t.TeamName, t.TeamDescription }).ToList();
            var companyTeamsJson = JsonSerializer.Serialize(companyTeams, new JsonSerializerOptions { WriteIndented = true });

            jsonContents.Add(companyTeamsJson); // Store JSON for Blob Storage

            processedFiles.Add(new ProcessedPretrainData
            {
                FileDescription = "List of company teams. Keywords: support teams, customer service, redirect to agent, escalation, assistance, live agent.",
                FileUrl = "",  // To be updated after uploading to Azure Blob Storage
                CompanyId = company.Id,
                FileType = FileType.CompanyTeams,
                ProcessedAt = DateTime.UtcNow
            });

            return (processedFiles, jsonContents);
        }



        ///// <summary>
        ///// Parses a CSV file stream into a structured dictionary.
        ///// </summary>
        //public static List<Dictionary<string, string>> ParseCsv(Stream fileStream)
        //{
        //    using var reader = new StreamReader(fileStream);
        //    using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
        //    var records = new List<Dictionary<string, string>>();

        //    foreach (var record in csv.GetRecords<dynamic>())
        //    {
        //        var rowDict = new Dictionary<string, string>();
        //        foreach (var property in ((IDictionary<string, object>)record))
        //        {
        //            rowDict[property.Key] = property.Value?.ToString() ?? string.Empty;
        //        }
        //        records.Add(rowDict);
        //    }
        //    return records;
        //}

        ///// <summary>
        ///// Parses an Excel file stream into a structured dictionary.
        ///// </summary>
        //public static List<Dictionary<string, string>> ParseExcel(Stream fileStream)
        //{
        //    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        //    var data = new List<Dictionary<string, string>>();

        //    using var package = new ExcelPackage(fileStream);
        //    var worksheet = package.Workbook.Worksheets[0]; // First sheet
        //    var rowCount = worksheet.Dimension.Rows;
        //    var colCount = worksheet.Dimension.Columns;

        //    var headers = new List<string>();
        //    for (int col = 1; col <= colCount; col++)
        //    {
        //        headers.Add(worksheet.Cells[1, col].Text.Trim());
        //    }

        //    for (int row = 2; row <= rowCount; row++)
        //    {
        //        var rowDict = new Dictionary<string, string>();
        //        for (int col = 1; col <= colCount; col++)
        //        {
        //            rowDict[headers[col - 1]] = worksheet.Cells[row, col].Text.Trim();
        //        }
        //        data.Add(rowDict);
        //    }
        //    return data;
        //}

        ///// <summary>
        ///// Processes extracted text from a document into structured JSON for Azure Search.
        ///// </summary>
        //public static string ProcessMetadataForAzureSearch(string extractedText)
        //{
        //    try
        //    {
        //        var knowledgeBase = new List<object>();

        //        if (DetectFAQPattern(extractedText))
        //        {
        //            var faqs = ExtractFAQs(extractedText);
        //            knowledgeBase.AddRange(faqs.Select(faq => new
        //            {
        //                id = $"faq-{Guid.NewGuid()}",
        //                type = "FAQ",
        //                question = faq.Key,
        //                answer = faq.Value
        //            }));
        //        }
        //        else
        //        {
        //            knowledgeBase.Add(new
        //            {
        //                id = $"doc-{Guid.NewGuid()}",
        //                type = "TextDocument",
        //                content = extractedText
        //            });
        //        }

        //        return JsonSerializer.Serialize(knowledgeBase, new JsonSerializerOptions { WriteIndented = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"🚨 Error processing metadata: {ex.Message}");
        //        return string.Empty;
        //    }
        //}

        //private static bool DetectFAQPattern(string text)
        //{
        //    return text.Contains("\nQ") && text.Contains("\nA:");
        //}

        //private static List<KeyValuePair<string, string>> ExtractFAQs(string text)
        //{
        //    var faqs = new List<KeyValuePair<string, string>>();
        //    var questions = text.Split("\nQ", StringSplitOptions.RemoveEmptyEntries);

        //    foreach (var question in questions)
        //    {
        //        var parts = question.Split("A:", 2);
        //        if (parts.Length == 2)
        //        {
        //            faqs.Add(new KeyValuePair<string, string>("Q" + parts[0].Trim(), parts[1].Trim()));
        //        }
        //    }
        //    return faqs;
        //}
    }
}
