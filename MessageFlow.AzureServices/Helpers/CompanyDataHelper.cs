using CsvHelper;
using CsvHelper.Configuration;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Shared.DTOs;
using MessageFlow.Shared.Enums;
using OfficeOpenXml;
using System.Globalization;
using System.Text.Json;

namespace MessageFlow.AzureServices.Helpers
{
    public static class CompanyDataHelper
    {

        /// <summary>
        /// Processes a list of uploaded files and extracts structured data into separate documents.
        /// </summary>
        public static async Task<(List<ProcessedPretrainDataDTO>, List<string>)> ProcessUploadedFilesAsync(
            List<PretrainDataFileDTO> uploadedFiles,
            IDocumentProcessingService documentProcessingService)
        {
            var processedFiles = new List<ProcessedPretrainDataDTO>();
            var jsonContents = new List<string>();

            foreach (var file in uploadedFiles)
            {
                using var memoryStream = new MemoryStream();
                await file.FileContent.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset position for reading

                string fileExtension = Path.GetExtension(file.FileName).ToLower();

                List<ProcessedPretrainDataDTO> generatedFiles = new();
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
        public static (List<ProcessedPretrainDataDTO>, List<string>) ConvertCsvToPretrainFiles(PretrainDataFileDTO originalFile, Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            var processedFiles = new List<ProcessedPretrainDataDTO>();
            var jsonContents = new List<string>();

            foreach (var record in csv.GetRecords<dynamic>())
            {
                var rowDict = (IDictionary<string, object>)record;

                // Convert the row into a structured JSON string
                var jsonContent = JsonSerializer.Serialize(rowDict, new JsonSerializerOptions { WriteIndented = true });

                // Add to JSON contents list
                jsonContents.Add(jsonContent);

                processedFiles.Add(new ProcessedPretrainDataDTO
                {
                    FileDescription = originalFile.FileDescription,
                    FileUrl = "",  // Will be set after uploading to Azure Blob Storage
                    CompanyId = originalFile.CompanyId,
                    FileType = FileType.CsvFile,
                    ProcessedAt = DateTime.UtcNow
                });
            }

            return (processedFiles, jsonContents);
        }



        /// <summary>
        /// Converts an Excel file stream into individual structured JSON documents.
        /// </summary>
        public static (List<ProcessedPretrainDataDTO>, List<string>) ConvertExcelToPretrainFiles(PretrainDataFileDTO originalFile, Stream fileStream)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            var processedFiles = new List<ProcessedPretrainDataDTO>();
            var jsonContents = new List<string>();
            using var package = new ExcelPackage(fileStream);

            // Loop through all sheets in the workbook
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
                    // Add to JSON contents list
                    jsonContents.Add(jsonContent);

                    processedFiles.Add(new ProcessedPretrainDataDTO
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
        public static (List<ProcessedPretrainDataDTO>, List<string>) ConvertTextToPretrainFiles(PretrainDataFileDTO originalFile, string extractedText)
        {
            var processedFiles = new List<ProcessedPretrainDataDTO>();
            var jsonContents = new List<string>();

            // If no FAQ pattern is detected, store the text as a general document
            if (!DetectFAQPattern(extractedText))
            {
                var document = new
                {
                    type = "TextDocument",
                    content = extractedText
                };

                var jsonContent = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });

                // Add JSON content to list
                jsonContents.Add(jsonContent);

                // Add metadata for DB storage
                processedFiles.Add(new ProcessedPretrainDataDTO
                {
                    FileDescription = originalFile.FileDescription,
                    FileUrl = "",  // To be updated after uploading to Azure Blob Storage
                    CompanyId = originalFile.CompanyId,
                    FileType = FileType.Other,
                    ProcessedAt = DateTime.UtcNow
                });

                return (processedFiles, jsonContents);
            }

            // If FAQ pattern is detected, extract and store each FAQ separately
            var faqs = ExtractFAQs(extractedText);
            foreach (var faq in faqs)
            {
                var faqDocument = new
                {
                    question = faq.Key,
                    answer = faq.Value
                };

                var jsonContent = JsonSerializer.Serialize(faqDocument, new JsonSerializerOptions { WriteIndented = true });

                // Add JSON content to list
                jsonContents.Add(jsonContent);

                // Add metadata for DB storage
                processedFiles.Add(new ProcessedPretrainDataDTO
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
        public static (List<ProcessedPretrainDataDTO>, List<string>) GenerateStructuredCompanyMetadata(CompanyDTO company)
        {
            var processedFiles = new List<ProcessedPretrainDataDTO>();
            var jsonContents = new List<string>();

            // General Company Information Document
            var generalInfo = new
            {
                Id = Guid.NewGuid().ToString(),
                company_name = company.CompanyName,
                company_description = company.Description,
                industry_type = company.IndustryType,
                company_website = company.WebsiteUrl
            };

            var generalInfoJson = JsonSerializer.Serialize(generalInfo, new JsonSerializerOptions { WriteIndented = true });

            jsonContents.Add(generalInfoJson);

            processedFiles.Add(new ProcessedPretrainDataDTO
            {
                Id = Guid.NewGuid().ToString(),
                FileDescription = "General information about the company, including industry type and website.",
                FileUrl = "",
                CompanyId = company.Id,
                FileType = FileType.CompanyDetails,
                ProcessedAt = DateTime.UtcNow
            });

            // Company Emails Document
            var companyEmails = company.CompanyEmails.Select(e => new { e.EmailAddress, e.Description }).ToList();
            var companyEmailsJson = JsonSerializer.Serialize(companyEmails, new JsonSerializerOptions { WriteIndented = true });

            jsonContents.Add(companyEmailsJson);

            processedFiles.Add(new ProcessedPretrainDataDTO
            {
                Id = Guid.NewGuid().ToString(),
                FileDescription = "Company contact emails. Use this for support, inquiries, or reaching out to relevant departments.",
                FileUrl = "",
                CompanyId = company.Id,
                FileType = FileType.CompanyEmails,
                ProcessedAt = DateTime.UtcNow
            });

            // Company Phone Numbers Document
            var companyPhones = company.CompanyPhoneNumbers.Select(p => new { p.PhoneNumber, p.Description }).ToList();
            var companyPhonesJson = JsonSerializer.Serialize(companyPhones, new JsonSerializerOptions { WriteIndented = true });

            jsonContents.Add(companyPhonesJson);

            processedFiles.Add(new ProcessedPretrainDataDTO
            {
                Id = Guid.NewGuid().ToString(),
                FileDescription = "Company contact phone numbers. Use this for customer support, direct contact, and urgent inquiries.",
                FileUrl = "",
                CompanyId = company.Id,
                FileType = FileType.CompanyPhoneNumbers,
                ProcessedAt = DateTime.UtcNow
            });

            // Company Teams Document
            var companyTeams = company.Teams.Select(t => new { t.Id, t.TeamName, t.TeamDescription }).ToList();
            var companyTeamsJson = JsonSerializer.Serialize(companyTeams, new JsonSerializerOptions { WriteIndented = true });

            jsonContents.Add(companyTeamsJson);

            processedFiles.Add(new ProcessedPretrainDataDTO
            {
                Id = Guid.NewGuid().ToString(),
                FileDescription = "List of company teams. Keywords: support teams, customer service, redirect to agent, escalation, assistance, live agent.",
                FileUrl = "",
                CompanyId = company.Id,
                FileType = FileType.CompanyTeams,
                ProcessedAt = DateTime.UtcNow
            });

            return (processedFiles, jsonContents);
        }
    }
}
