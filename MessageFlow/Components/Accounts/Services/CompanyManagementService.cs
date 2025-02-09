using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using MessageFlow.Components.AzureServices;
using MessageFlow.Components.Accounts.Helpers;
using System.Text;


namespace MessageFlow.Components.Accounts.Services
{
    public class CompanyManagementService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<CompanyManagementService> _logger;
        private readonly ClaimsPrincipal _currentUser;
        private readonly TeamsManagementService _teamsManagementService;
        private readonly AzureBlobStorageService _blobStorageService;
        private readonly DocumentProcessingService _documentProcessingService;
        private readonly AzureSearchService _azureSearchService;

        public CompanyManagementService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<CompanyManagementService> logger,
            IHttpContextAccessor httpContextAccessor,
            TeamsManagementService teamsManagementService,
            AzureBlobStorageService blobStorageService,
            DocumentProcessingService documentProcessingService,
            AzureSearchService azureSearchService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _currentUser = httpContextAccessor.HttpContext?.User ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _teamsManagementService = teamsManagementService ?? throw new ArgumentNullException(nameof(teamsManagementService));
            _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
            _documentProcessingService = documentProcessingService;
            _azureSearchService = azureSearchService;
        }


        // Fetch a specific company by ID
        public async Task<Company?> GetCompanyByIdAsync(ApplicationDbContext dbContext, int companyId)
        {
            return await dbContext.Companies
                .Include(c => c.CompanyEmails)
                .Include(c => c.CompanyPhoneNumbers)
                .Include(c => c.PretrainDataFiles)
                .Include(c => c.Teams)
                    .ThenInclude(t => t.UserTeams)
                    .ThenInclude(ut => ut.User)
                .FirstOrDefaultAsync(c => c.Id == companyId);
        }

        // Fetch all companies with the total number of associated users
        public async Task<List<Company>> GetAllCompaniesAsync()
        {
            await using var dbContext = _contextFactory.CreateDbContext();
            var companies = await dbContext.Companies
                .Select(c => new Company
                {
                    Id = c.Id,
                    AccountNumber = c.AccountNumber,
                    CompanyName = c.CompanyName,
                })
                .ToListAsync();

            return companies;
        }

        public async Task<string?> GetCompanyNameByIdAsync(int companyId)
        {
            await using var dbContext = _contextFactory.CreateDbContext();
            var company = await dbContext.Companies
                .Where(c => c.Id == companyId)
                .Select(c => c.CompanyName)
                .FirstOrDefaultAsync();

            return company;
        }

        public async Task<Company?> GetCompanyForUserAsync(ClaimsPrincipal user)
        {
            await using var dbContext = _contextFactory.CreateDbContext();
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            // First, get the company ID of the user
            var companyId = await dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => u.CompanyId)
                .FirstOrDefaultAsync();

            if (companyId == 0) // If the companyId is invalid, return null
            {
                return null;
            }

            // Now fetch the actual Company object by its Id
            var company = await GetCompanyByIdAsync(dbContext, companyId);
            return company;
        }

        // Fetch all users for a specific company
        public async Task<List<ApplicationUser>> GetUsersForCompanyAsync(int companyId)
        {
            await using var dbContext = _contextFactory.CreateDbContext();

            var users = await dbContext.Users
                .Where(u => u.CompanyId == companyId)
                .ToListAsync();

            return users;
        }

        // Create a new company
        public async Task<(bool success, string errorMessage)> CreateCompanyAsync(Company company)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(0); // 0 as companyId (irrelevant for creation)
                if (!isSuperAdmin) // Only SuperAdmins can create companies
                {
                    return (false, "Only SuperAdmins can create companies.");
                }

                await using var dbContext = _contextFactory.CreateDbContext();

                dbContext.Companies.Add(company);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Company {company.CompanyName} created successfully.");
                return (true, "Company created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return (false, "An error occurred while creating the company.");
            }
        }

        // Delete a company
        public async Task<(bool success, string errorMessage)> DeleteCompanyAsync(int companyId)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyId);
                if (!isSuperAdmin) // Only SuperAdmins can delete companies
                {
                    return (false, "Only SuperAdmins can delete companies.");
                }

                await using var dbContext = _contextFactory.CreateDbContext();

                var company = await GetCompanyByIdAsync(dbContext, companyId);

                if (company == null)
                {
                    return (false, "Company not found.");
                }

                // Get all users associated with the company
                var users = await dbContext.Users.Where(u => u.CompanyId == companyId).ToListAsync();

                // Remove all related UserRoles, UserClaims, UserLogins, UserTokens for each user
                foreach (var user in users)
                {
                    var userRoles = await dbContext.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync();
                    dbContext.UserRoles.RemoveRange(userRoles);

                    var userClaims = await dbContext.UserClaims.Where(uc => uc.UserId == user.Id).ToListAsync();
                    dbContext.UserClaims.RemoveRange(userClaims);

                    var userLogins = await dbContext.UserLogins.Where(ul => ul.UserId == user.Id).ToListAsync();
                    dbContext.UserLogins.RemoveRange(userLogins);

                    var userTokens = await dbContext.UserTokens.Where(ut => ut.UserId == user.Id).ToListAsync();
                    dbContext.UserTokens.RemoveRange(userTokens);

                    // Remove the user from the UserTeams relationship
                    var userTeams = await dbContext.UserTeams.Where(ut => ut.UserId == user.Id).ToListAsync();
                    dbContext.UserTeams.RemoveRange(userTeams);

                    // Finally, remove the user
                    dbContext.Users.Remove(user);
                }

                // Delete all teams associated with the company via TeamsManagementService
                await _teamsManagementService.DeleteTeamsByCompanyIdAsync(companyId);

                // Remove the company itself
                dbContext.Companies.Remove(company);

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Company {company.CompanyName} and all associated data deleted successfully.");
                return (true, "Company and all associated data deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company");
                return (false, "An error occurred while deleting the company.");
            }
        }

        // Company Details
        public async Task<(bool success, string errorMessage)> UpdateCompanyDetailsAsync(Company company)
        {
            try
            {
                var (isAuthorized, userCompanyId, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(company.Id);
                if (!isAuthorized)
                {
                    return (false, errorMessage);
                }

                await using var dbContext = _contextFactory.CreateDbContext();

                var userId = _currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRoles = await dbContext.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToListAsync();

                var existingCompany = await dbContext.Companies
                    .Where(c => c.Id == company.Id)
                    .FirstOrDefaultAsync();

                if (existingCompany == null)
                {
                    return (false, "Company not found.");
                }

                if (!isSuperAdmin)
                {
                    // Prevent Admins from modifying restricted fields
                    existingCompany.CompanyName = company.CompanyName;
                    existingCompany.Description = company.Description;
                    existingCompany.IndustryType = company.IndustryType;
                    existingCompany.WebsiteUrl = company.WebsiteUrl;
                }
                else
                {
                    // SuperAdmins can modify all fields freely
                    existingCompany.CompanyName = company.CompanyName;
                    existingCompany.AccountNumber = company.AccountNumber;
                    existingCompany.Description = company.Description;
                    existingCompany.IndustryType = company.IndustryType;
                    existingCompany.WebsiteUrl = company.WebsiteUrl;
                }

                dbContext.Companies.Update(existingCompany);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"Company {company.CompanyName} details updated successfully.");
                return (true, "Company details updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company details");
                return (false, "An error occurred while updating company details.");
            }
        }

        // Company Emails
        public async Task<(bool success, string errorMessage)> UpdateCompanyEmailsAsync(Company company)
        {
            try
            {
                var (isAuthorized, userCompanyId, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(company.Id);
                if (!isAuthorized)
                {
                    return (false, errorMessage);
                }

                await using var dbContext = _contextFactory.CreateDbContext();

                var existingCompany = await dbContext.Companies
                    .Include(c => c.CompanyEmails)
                    .FirstOrDefaultAsync(c => c.Id == company.Id);

                if (existingCompany == null)
                {
                    return (false, "Company not found.");
                }

                existingCompany.CompanyEmails = company.CompanyEmails;

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Company emails updated for {company.CompanyName}.");
                return (true, "Company emails updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company emails");
                return (false, "An error occurred while updating company emails.");
            }
        }

        // Company Phone Numbers
        public async Task<(bool success, string errorMessage)> UpdateCompanyPhoneNumbersAsync(Company company)
        {
            try
            {
                var (isAuthorized, userCompanyId, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(company.Id);
                if (!isAuthorized)
                {
                    return (false, errorMessage);
                }

                await using var dbContext = _contextFactory.CreateDbContext();

                var existingCompany = await dbContext.Companies
                    .Include(c => c.CompanyPhoneNumbers)
                    .FirstOrDefaultAsync(c => c.Id == company.Id);

                if (existingCompany == null)
                {
                    return (false, "Company not found.");
                }

                existingCompany.CompanyPhoneNumbers = company.CompanyPhoneNumbers;

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Company phone numbers updated for {company.CompanyName}.");
                return (true, "Company phone numbers updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company phone numbers");
                return (false, "An error occurred while updating company phone numbers.");
            }
        }

        // Retrieve company metadata
        public async Task<(bool success, string metadata, string errorMessage)> GetCompanyMetadataAsync(int companyId)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyId);
                if (!isAuthorized)
                {
                    return (false, string.Empty, errorMessage);
                }

                await using var dbContext = _contextFactory.CreateDbContext();
                // Look for the structured data file instead of "metadata.json"
                string structuredFileName = $"company_{companyId}_structured_data.json";
                var metadataFile = await dbContext.PretrainDataFiles
                    .Where(f => f.CompanyId == companyId && f.FileName == structuredFileName)
                    .Select(f => f.FileUrl)
                    .FirstOrDefaultAsync();

                if (metadataFile == null)
                {
                    return (false, string.Empty, "Metadata not found.");
                }

                var metadataContent = await _blobStorageService.DownloadFileContentAsync(metadataFile);
                return (true, metadataContent, "Metadata retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving metadata for company {CompanyId}", companyId);
                return (false, string.Empty, "An error occurred while retrieving metadata.");
            }
        }

        public async Task<(bool success, string errorMessage)> GenerateAndUploadCompanyMetadataAsync(int companyId)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyId);
                if (!isAuthorized)
                {
                    return (false, errorMessage);
                }

                await using var dbContext = _contextFactory.CreateDbContext();
                var uploadedFiles = await dbContext.PretrainDataFiles
                    .Where(f => f.CompanyId == companyId)
                    .ToListAsync();

                // 🔹 Extract structured data from files
                var processedDocuments = await ProcessUploadedFiles(uploadedFiles);

                var company = await dbContext.Companies
                    .Include(c => c.CompanyEmails)
                    .Include(c => c.CompanyPhoneNumbers)
                    .Include(c => c.Teams)
                    .FirstOrDefaultAsync(c => c.Id == companyId);

                if (company == null)
                {
                    return (false, "Company not found.");
                }

                var metadata = new
                {
                    company.Id,
                    company.CompanyName,
                    company.Description,
                    company.IndustryType,
                    company.WebsiteUrl,
                    Emails = company.CompanyEmails.Select(e => new { e.EmailAddress, e.Description }),
                    PhoneNumbers = company.CompanyPhoneNumbers.Select(p => new { p.PhoneNumber, p.Description }),
                    Teams = company.Teams.Select(t => new { t.Id, t.TeamName, t.TeamDescription })
                };

                // 🔹 Use JsonStructureHelper instead of manually building JSON
                string jsonContent = JsonSerializer.Serialize(new { Company = metadata, Documents = processedDocuments }, new JsonSerializerOptions { WriteIndented = true });

                // 🔹 Extract JSON structure dynamically
                var structuredJson = JsonStructureHelper.ExtractJsonStructure(jsonContent);
                if (structuredJson.Count == 0)
                {
                    return (false, "Error: Extracted JSON structure is empty.");
                }

                using var finalJsonStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(structuredJson, new JsonSerializerOptions { WriteIndented = true })));

                string finalFileName = $"company_{companyId}_structured_data.json";
                string finalFileUrl = await _blobStorageService.UploadFileAsync(finalJsonStream, finalFileName, "application/json", companyId);

                dbContext.PretrainDataFiles.Add(new PretrainDataFile
                {
                    FileName = finalFileName,
                    FileUrl = finalFileUrl,
                    FileDescription = "Company Metadata",
                    CompanyId = company.Id
                });

                await dbContext.SaveChangesAsync();

                return (true, "Company data converted to a structured document and updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating and uploading metadata for company {CompanyId}", companyId);
                return (false, "An error occurred while generating metadata.");
            }
        }


        private async Task<List<object>> ProcessUploadedFiles(List<PretrainDataFile> uploadedFiles)
        {
            var processedDocuments = new List<object>();

            foreach (var file in uploadedFiles)
            {
                // Skip existing metadata file
                if (file.FileName.EndsWith("_structured_data.json")) continue;

                using var fileStream = await _blobStorageService.DownloadFileAsStreamAsync(file.FileUrl);
                string fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (fileExtension == ".csv")
                {
                    var parsedData = CompanyDataHelper.ParseCsv(fileStream);
                    if (parsedData.Any())
                    {
                        processedDocuments.Add(new { file.FileName, StructuredData = parsedData });
                    }
                }
                else if (fileExtension == ".xlsx")
                {
                    var parsedData = CompanyDataHelper.ParseExcel(fileStream);
                    if (parsedData.Any())
                    {
                        processedDocuments.Add(new { file.FileName, StructuredData = parsedData });
                    }
                }
                else
                {
                    string extractedText = await _documentProcessingService.ExtractTextFromDocumentAsync(fileStream, "application/octet-stream");
                    string processedJson = CompanyDataHelper.ProcessMetadataForAzureSearch(extractedText);
                    var parsedExtractedText = JsonSerializer.Deserialize<object>(processedJson);
                    processedDocuments.Add(new { file.FileName, ExtractedText = parsedExtractedText });
                }
            }

            return processedDocuments;
        }


        public async Task<(bool success, string errorMessage)> CreateIndexFromMetadataAsync(int companyId)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                string structuredFileName = $"company_{companyId}_structured_data.json";
                var metadataFileUrl = await dbContext.PretrainDataFiles
                    .Where(f => f.CompanyId == companyId && f.FileName == structuredFileName)
                    .Select(f => f.FileUrl)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(metadataFileUrl))
                {
                    return (false, "Metadata file not found for this company.");
                }

                string jsonContent = await _blobStorageService.DownloadFileContentAsync(metadataFileUrl);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    return (false, "Failed to retrieve metadata content.");
                }

                // Extract document-specific fields dynamically
                //var dynamicDocumentFields = ExtractJsonStructure(jsonContent);
                var dynamicDocumentFields = JsonStructureHelper.ExtractJsonStructure(jsonContent);


                // Create the index dynamically
                await _azureSearchService.CreateCompanyIndexAsync(companyId, dynamicDocumentFields);

                // Convert JSON to searchable objects
                //var documentObjects = ExtractJsonStructure(jsonContent);
                var documentObjects = JsonStructureHelper.ExtractJsonStructure(jsonContent);
                if (documentObjects.Count == 0)
                {
                    return (false, "No valid documents extracted from metadata.");
                }

                // ✅ Upload extracted documents to the index
                await _azureSearchService.UploadDocumentsToIndexAsync(companyId, documentObjects);

                return (true, "Index created and populated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating index for company {CompanyId}", companyId);
                return (false, "An error occurred while creating the index.");
            }
        }

        public async Task<(bool success, string errorMessage)> DeleteCompanyMetadataAsync(int companyId)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyId);
                if (!isAuthorized)
                {
                    return (false, errorMessage);
                }

                await using var dbContext = _contextFactory.CreateDbContext();
                var existingMetadata = await dbContext.PretrainDataFiles
                    .FirstOrDefaultAsync(f => f.CompanyId == companyId && f.FileName == "metadata.json");

                if (existingMetadata == null)
                {
                    return (false, "Metadata not found.");
                }

                bool deletedFromStorage = await _blobStorageService.DeleteFileAsync(existingMetadata.FileUrl);
                if (deletedFromStorage)
                {
                    dbContext.PretrainDataFiles.Remove(existingMetadata);
                    await dbContext.SaveChangesAsync();
                    return (true, "Company metadata deleted successfully.");
                }

                return (false, "Failed to delete metadata.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting metadata for company {CompanyId}", companyId);
                return (false, "An error occurred while deleting metadata.");
            }
        }


        // Company Pretraining Files
        // Fetch Existing Pretraining Files
        public async Task<(bool success, List<PretrainDataFile> files, string errorMessage)> GetCompanyPretrainingFilesAsync(int companyId)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();
                var files = await dbContext.PretrainDataFiles.Where(f => f.CompanyId == companyId).ToListAsync();
                return (true, files, "Files retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pretraining files for company {CompanyId}", companyId);
                return (false, new List<PretrainDataFile>(), "An error occurred while retrieving files.");
            }
        }

        // Upload Multiple Files
        public async Task<(bool success, string errorMessage)> UploadCompanyFilesAsync(int companyId, List<PretrainDataFile> files)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();
                var company = await dbContext.Companies.FindAsync(companyId);
                if (company == null)
                {
                    return (false, "Company not found.");
                }

                foreach (var file in files)
                {
                    string fileUrl = await _blobStorageService.UploadFileAsync(file.FileContent, file.FileName, "application/octet-stream", companyId);
                    if (string.IsNullOrEmpty(fileUrl))
                    {
                        return (false, "File upload failed.");
                    }

                    dbContext.PretrainDataFiles.Add(new PretrainDataFile
                    {
                        FileName = file.FileName,
                        FileUrl = fileUrl,
                        FileDescription = file.FileDescription,
                        CompanyId = companyId
                    });
                }

                await dbContext.SaveChangesAsync();
                return (true, "Files uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading pretraining files for company {CompanyId}", companyId);
                return (false, "An error occurred during file upload.");
            }
        }

        public async Task<bool> DeleteCompanyFileAsync(int companyId, string fileUrl)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                var fileRecord = await dbContext.PretrainDataFiles
                    .FirstOrDefaultAsync(f => f.CompanyId == companyId && f.FileUrl == fileUrl);

                if (fileRecord == null)
                {
                    return false; // File not found in DB
                }

                // Step 1: Delete from Azure Blob Storage
                bool deletedFromStorage = await _blobStorageService.DeleteFileAsync(fileUrl);

                if (deletedFromStorage)
                {
                    // Step 2: Remove DB entry
                    dbContext.PretrainDataFiles.Remove(fileRecord);
                    await dbContext.SaveChangesAsync();
                }

                return deletedFromStorage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file for company {CompanyId}", companyId);
                return false;
            }
        }

        private async Task<(bool isAuthorized, int? userCompanyId, bool isSuperAdmin, string errorMessage)> CanUserEditCompanyAsync(int companyId)
        {
            await using var dbContext = _contextFactory.CreateDbContext();

            var userId = _currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRoles = await dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            var isSuperAdmin = userRoles.Contains("SuperAdmin");

            if (isSuperAdmin)
            {
                return (true, null, true, string.Empty); // SuperAdmins can modify all companies
            }

            var userCompanyId = await dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => u.CompanyId)
                .FirstOrDefaultAsync();

            if (userCompanyId == null || companyId != userCompanyId)
            {
                return (false, userCompanyId, false, "You are not authorized to update this company.");
            }

            return (true, userCompanyId, false, string.Empty);
        }


        public ApplicationDbContext CreateDbContext()
        {
            return _contextFactory.CreateDbContext();
        }

    }
}
