using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using MessageFlow.Components.AzureServices;
using MessageFlow.Components.Accounts.Helpers;
using System.Text;
using System.ComponentModel.Design;


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

        // To be changed to GetAllCompanyMetaData (display entire json of file used to create the index)
        public async Task<(bool success, string metadata, string errorMessage)> GetCompanyMetadataAsync(int companyId)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyId);
                if (!isAuthorized)
                {
                    return (false, string.Empty, errorMessage);
                }

                // 🔹 Use new method to retrieve all JSON files from CompanyRAGData folder
                string metadataContent = await _blobStorageService.GetAllCompanyRagDataFilesAsync(companyId);

                if (string.IsNullOrEmpty(metadataContent))
                {
                    return (false, string.Empty, "Metadata not found.");
                }

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
                            
                var company = await dbContext.Companies
                    .Include(c => c.CompanyEmails)
                    .Include(c => c.CompanyPhoneNumbers)
                    .Select(c => new Company
                    {
                        Id = c.Id,
                        AccountNumber = c.AccountNumber,
                        CompanyName = c.CompanyName,
                        CompanyEmails = c.CompanyEmails,
                        CompanyPhoneNumbers = c.CompanyPhoneNumbers,
                        Teams = c.Teams.Select(t => new Team
                        {
                            Id = t.Id,
                            TeamName = t.TeamName,
                            TeamDescription = t.TeamDescription
                        }).ToList()
                    })
                    .FirstOrDefaultAsync(c => c.Id == companyId);


                if (company == null)
                {
                    return (false, "Company not found.");
                }

                // 🔹 1. Retrieve existing metadata records from the database
                var existingFiles = await dbContext.ProcessedPretrainData
                    .Where(f => f.CompanyId == companyId &&
                                (f.FileType == FileType.CompanyEmails ||
                                 f.FileType == FileType.CompanyDetails ||
                                 f.FileType == FileType.CompanyPhoneNumbers))
                    .ToListAsync();

                if (existingFiles.Any())
                {
                    Console.WriteLine($"🗑 Found {existingFiles.Count} existing metadata files. Deleting...");

                    // 🔹 2. Delete files from Azure Blob Storage
                    foreach (var file in existingFiles)
                    {
                        if (!string.IsNullOrEmpty(file.FileUrl))
                        {
                            await _blobStorageService.DeleteFileAsync(file.FileUrl);
                            Console.WriteLine($"✅ Deleted from Blob Storage: {file.FileUrl}");
                        }
                    }

                    // 🔹 3. Remove records from the database
                    dbContext.ProcessedPretrainData.RemoveRange(existingFiles);
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine("🗑 Old metadata records deleted from database.");
                }

                Console.WriteLine($"🚀 Uploading new metadata files for Company ID {companyId}...");

                // 🔹 Generate structured metadata documents and JSON contents
                var (processedFiles, jsonContents) = CompanyDataHelper.GenerateStructuredCompanyMetadata(company);

                if (processedFiles.Count != jsonContents.Count)
                {
                    return (false, "Mismatch between processed metadata files and JSON contents.");
                }

                // `CompanyRAGData/` folder exists in blob storage
                string baseFolderPath = $"CompanyRAGData/";

                // 🔹 Upload JSON contents to Azure Blob Storage and link them to database entries
                for (int i = 0; i < processedFiles.Count; i++)
                {
                    var processedFile = processedFiles[i];
                    var jsonContent = jsonContents[i];

                    using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
                    string jsonFileName = $"{baseFolderPath}company_{companyId}_pretrain_{processedFile.Id}.json";
                    string jsonFileUrl = await _blobStorageService.UploadFileAsync(jsonStream, jsonFileName, "application/json", companyId);

                    // 🔹 Step 3: Update `FileUrl` after uploading to Azure Blob Storage
                    processedFile.FileUrl = jsonFileUrl;
                }

                // 🔹 Step 4: Store metadata in the database
                await dbContext.ProcessedPretrainData.AddRangeAsync(processedFiles);
                await dbContext.SaveChangesAsync();

                return (true, "Company metadata structured and uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating and uploading metadata for company {CompanyId}", companyId);
                return (false, "An error occurred while generating metadata.");
            }
        }

        public async Task<(bool success, string errorMessage)> CreateAzureAiSearchIndexAndUploadFilesAsync(int companyId)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                // 🔹 Retrieve all processed pretrain data for the company
                var processedFiles = await dbContext.ProcessedPretrainData
                    .Where(f => f.CompanyId == companyId)
                    .ToListAsync();

                if (!processedFiles.Any())
                {
                    return (false, "No processed data found for this company.");
                }                

                // Create the index dynamically
                await _azureSearchService.CreateCompanyIndexAsync(companyId);                

                // ✅ Upload structured documents to the index
                await _azureSearchService.UploadDocumentsToIndexAsync(companyId, processedFiles);

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

                // 🔹 Find all metadata files related to the company
                var existingMetadataFiles = await dbContext.ProcessedPretrainData
                    .Where(f => f.CompanyId == companyId)
                    .ToListAsync();

                if (!existingMetadataFiles.Any())
                {
                    return (false, "No metadata files found for this company.");
                }

                // 🔹 Step 1: Attempt to delete files from Azure Blob Storage
                var successfullyDeletedFiles = new List<ProcessedPretrainData>();
                bool allFilesDeleted = true;

                foreach (var file in existingMetadataFiles)
                {
                    if (!string.IsNullOrEmpty(file.FileUrl))
                    {
                        bool deleted = await _blobStorageService.DeleteFileAsync(file.FileUrl);
                        if (deleted)
                        {
                            successfullyDeletedFiles.Add(file); // Mark for removal from the database
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to delete file from Blob Storage: {file.FileUrl}");
                            allFilesDeleted = false; // Track failure
                        }
                    }
                }

                // 🔹 Step 2: Remove only successfully deleted records from the database
                if (successfullyDeletedFiles.Any())
                {
                    dbContext.ProcessedPretrainData.RemoveRange(successfullyDeletedFiles);
                    await dbContext.SaveChangesAsync();
                }

                if (allFilesDeleted)
                {
                    return (true, "All company metadata files deleted successfully.");
                }
                return (false, "Some files failed to delete from Azure Blob Storage, their database records were retained.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting metadata for company {CompanyId}", companyId);
                return (false, "An error occurred while deleting metadata.");
            }
        }




        // Company Pretraining Files
        // Fetch Existing Pretraining Files
        public async Task<(bool success, List<ProcessedPretrainData> files, string errorMessage)> GetCompanyPretrainingFilesAsync(int companyId)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();
                var files = await dbContext.ProcessedPretrainData.Where(f => f.CompanyId == companyId).ToListAsync();
                return (true, files, "Files retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pretraining files for company {CompanyId}", companyId);
                return (false, new List<ProcessedPretrainData>(), "An error occurred while retrieving files.");
            }
        }

        // Upload Multiple Files
        public async Task<(bool success, string errorMessage)> UploadCompanyFilesAsync(List<PretrainDataFile> files)
        {
            var firstFile = files.FirstOrDefault();
            int companyId = firstFile?.CompanyId ?? 0;
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();
                var company = await dbContext.Companies.FindAsync(companyId);
                if (company == null)
                {
                    return (false, "Company not found.");
                }

                // **Define base folder path**
                string baseFolderPath = $"CompanyRAGData/";

                // 🔹 **Step 1: Find and delete existing files of the specified types**
                var existingFiles = await dbContext.ProcessedPretrainData
                    .Where(f => f.CompanyId == companyId &&
                                (f.FileType == FileType.CsvFile ||
                                 f.FileType == FileType.ExcelFile ||
                                 f.FileType == FileType.FAQFile ||
                                 f.FileType == FileType.Other))
                    .ToListAsync();

                if (existingFiles.Any())
                {
                    Console.WriteLine($"🗑 Found {existingFiles.Count} existing files of type CSV, Excel, FAQ, or Other. Deleting...");

                    foreach (var file in existingFiles)
                    {
                        if (!string.IsNullOrEmpty(file.FileUrl))
                        {
                            await _blobStorageService.DeleteFileAsync(file.FileUrl);
                            Console.WriteLine($"✅ Deleted from Blob Storage: {file.FileUrl}");
                        }
                    }

                    dbContext.ProcessedPretrainData.RemoveRange(existingFiles);
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine("🗑 Old files deleted from database.");
                }

                Console.WriteLine($"🚀 Uploading new files for Company ID {companyId}...");


                // 🔹 Step 1: Process files and extract structured documents
                var (processedFiles, jsonContents) = await CompanyDataHelper.ProcessUploadedFilesAsync(files, _documentProcessingService);

                if (processedFiles.Count != jsonContents.Count)
                {
                    return (false, "Mismatch between processed files and JSON contents.");
                }

                // 🔹 Step 2: Upload JSON contents to Azure Blob Storage and link them to database entries
                for (int i = 0; i < processedFiles.Count; i++)
                {
                    var processedFile = processedFiles[i];
                    var jsonContent = jsonContents[i];

                    using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
                    string jsonFileName = $"{baseFolderPath}company_{companyId}_pretrain_{processedFile.Id}.json";
                    string jsonFileUrl = await _blobStorageService.UploadFileAsync(jsonStream, jsonFileName, "application/json", companyId);

                    // 🔹 Step 3: Update `FileUrl` after uploading to Azure Blob Storage
                    processedFile.FileUrl = jsonFileUrl;
                }

                // 🔹 Step 4: Store metadata in the database
                await dbContext.ProcessedPretrainData.AddRangeAsync(processedFiles);
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

                var fileRecord = await dbContext.ProcessedPretrainData
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
                    dbContext.ProcessedPretrainData.Remove(fileRecord);
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
