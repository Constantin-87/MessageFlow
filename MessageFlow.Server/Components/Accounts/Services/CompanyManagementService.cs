using MessageFlow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MessageFlow.AzureServices.Services;
using MessageFlow.AzureServices.Helpers;
using System.Text;
using AutoMapper;
using MessageFlow.Shared.DTOs;
using MessageFlow.Shared.Enums;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;
using System.ComponentModel.Design;
using System.Net.Http;


namespace MessageFlow.Server.Components.Accounts.Services
{
    public class CompanyManagementService
    {
        private readonly HttpClient _httpClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CompanyManagementService> _logger;
        private readonly ClaimsPrincipal _currentUser;
        private readonly TeamsManagementService _teamsManagementService;
        private readonly AzureBlobStorageService _blobStorageService;
        private readonly DocumentProcessingService _documentProcessingService;
        private readonly AzureSearchService _azureSearchService;
        private readonly IMapper _mapper;

        public CompanyManagementService(
            HttpClient httpClient,
            IUnitOfWork unitOfWork,
            ILogger<CompanyManagementService> logger,
            IHttpContextAccessor httpContextAccessor,
            TeamsManagementService teamsManagementService,
            AzureBlobStorageService blobStorageService,
            DocumentProcessingService documentProcessingService,
            AzureSearchService azureSearchService,
            IMapper mapper)
        {
            _httpClient = httpClient;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUser = httpContextAccessor.HttpContext?.User ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _teamsManagementService = teamsManagementService ?? throw new ArgumentNullException(nameof(teamsManagementService));
            _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
            _documentProcessingService = documentProcessingService;
            _azureSearchService = azureSearchService;
            _mapper = mapper;
        }

        // ✅ Fetch company using UnitOfWork & return CompanyDTO
        public async Task<CompanyDTO?> GetCompanyByIdAsync(string companyId)
        {
            var company = await _unitOfWork.Companies.GetCompanyWithDetailsByIdAsync(companyId);
            return company != null ? _mapper.Map<CompanyDTO>(company) : null;
        }

        // Fetch all companies with the total number of associated users
        //public async Task<List<CompanyDTO>> GetAllCompaniesAsync()
        //{
        //    var companies = await _unitOfWork.Companies.GetAllCompaniesWithUserCountAsync();
        //    return _mapper.Map<List<CompanyDTO>>(companies);
        //}

        public async Task<List<CompanyDTO>> GetAllCompaniesAsync()
        {
            if (_currentUser == null)
            {
                _logger.LogWarning("HTTP context or user is null while fetching companies.");
                return new List<CompanyDTO>();
            }

            // ✅ Retrieve roles from the HTTP context claims
            var userRoles = _currentUser.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var isSuperAdmin = userRoles.Contains("SuperAdmin");

            // ✅ Get the current user's CompanyId from claims
            var companyId = _currentUser.Claims
                .FirstOrDefault(c => c.Type == "CompanyId")?.Value;

            if (string.IsNullOrEmpty(companyId))
            {
                _logger.LogWarning("CompanyId claim not found for the current user.");
                return new List<CompanyDTO>();
            }

            if (isSuperAdmin)
            {
                // ✅ SuperAdmin: Return all companies
                var companies = await _unitOfWork.Companies.GetAllCompaniesWithUserCountAsync();
                return _mapper.Map<List<CompanyDTO>>(companies);
            }
            else
            {
                // ✅ Regular user: Return only the current user's company
                var company = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
                if (company == null)
                {
                    var errorMessage = $"Company with ID {companyId} not found.";
                    _logger.LogError(errorMessage);
                    throw new KeyNotFoundException(errorMessage);
                }

                return new List<CompanyDTO> { _mapper.Map<CompanyDTO>(company) };
            }
        }


        public async Task<CompanyDTO?> GetCompanyForUserAsync(ClaimsPrincipal user)
        {
            var userCompanyId = user.FindFirstValue("CompanyId");
            if (string.IsNullOrEmpty(userCompanyId))
            {
                return null;
            }

            // ✅ Use UnitOfWork to fetch the company details
            var company = await _unitOfWork.Companies.GetByIdStringAsync(userCompanyId);
            if (company == null)
            {
                return null;
            }

            // ✅ Map to CompanyDTO and return
            return _mapper.Map<CompanyDTO>(company);
        }

        // Fetch all users for a specific company
        //public async Task<List<ApplicationUserDTO>> GetUsersForCompanyAsync(string companyId)
        //{
        //    // ✅ Use UnitOfWork to get users by company
        //    var users = await _unitOfWork.ApplicationUsers.GetUsersForCompanyAsync(companyId);

        //    // ✅ Map to DTOs
        //    return _mapper.Map<List<ApplicationUserDTO>>(users);
        //}


        // Create a new company
        public async Task<(bool success, string errorMessage)> CreateCompanyAsync(CompanyDTO companyDto)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(""); // 0 as companyId (irrelevant for creation)
                if (!isSuperAdmin) // Only SuperAdmins can create companies
                {
                    return (false, "Only SuperAdmins can create companies.");
                }

                var company = _mapper.Map<Company>(companyDto);

                company.Id ??= Guid.NewGuid().ToString();
                // Prevent EF from saving empty collections
                company.CompanyEmails = null;
                company.CompanyPhoneNumbers = null;
                company.Users = null;
                company.Teams = null;
                company.PretrainDataFiles = null;

                await _unitOfWork.Companies.AddEntityAsync(company);
                await _unitOfWork.SaveChangesAsync();

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
        public async Task<(bool success, string errorMessage)> DeleteCompanyAsync(string companyId)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyId);
                if (!isSuperAdmin) // Only SuperAdmins can delete companies
                {
                    return (false, "Only SuperAdmins can delete companies.");
                }

                // ✅ Fetch the company using the repository
                var company = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
                if (company == null)
                {
                    return (false, "Company not found.");
                }

                // ✅ Remove all users associated with this company
                // 🔹 Call Identity API to delete all users for a given company
                var response = await _httpClient.DeleteAsync($"api/user-management/delete-company-users/{companyId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to delete users for company {companyId} via Identity API.");
                    return (false, "Failed to delete users for this company.");
                }

                //var users = await _unitOfWork.ApplicationUsers.GetUsersForCompanyAsync(companyId);
                //foreach (var user in users)
                //{
                //    await _unitOfWork.ApplicationUsers.DeleteUserByIdAsync(user.Id);
                //}

                // ✅ Delete all teams associated with the company via repository
                await _teamsManagementService.DeleteTeamsByCompanyIdAsync(companyId);

                // ✅ Remove the company itself
                await _unitOfWork.Companies.RemoveEntityAsync(company);

                // ✅ Save changes
                await _unitOfWork.SaveChangesAsync();

                //// Get all users associated with the company
                //var users = await dbContext.Users.Where(u => u.CompanyId == companyId).ToListAsync();

                //// Remove all related UserRoles, UserClaims, UserLogins, UserTokens for each user
                //foreach (var user in users)
                //{
                //    var userRoles = await dbContext.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync();
                //    dbContext.UserRoles.RemoveRange(userRoles);

                //    var userClaims = await dbContext.UserClaims.Where(uc => uc.UserId == user.Id).ToListAsync();
                //    dbContext.UserClaims.RemoveRange(userClaims);

                //    var userLogins = await dbContext.UserLogins.Where(ul => ul.UserId == user.Id).ToListAsync();
                //    dbContext.UserLogins.RemoveRange(userLogins);

                //    var userTokens = await dbContext.UserTokens.Where(ut => ut.UserId == user.Id).ToListAsync();
                //    dbContext.UserTokens.RemoveRange(userTokens);

                //    // Remove the user from the UserTeams relationship
                //    var userTeams = await dbContext.UserTeams.Where(ut => ut.UserId == user.Id).ToListAsync();
                //    dbContext.UserTeams.RemoveRange(userTeams);

                //    // Finally, remove the user
                //    dbContext.Users.Remove(user);
                //}

                //// Delete all teams associated with the company via TeamsManagementService
                //await _teamsManagementService.DeleteTeamsByCompanyIdAsync(companyId);

                //// Remove the company itself
                //dbContext.Companies.Remove(company);

                //await dbContext.SaveChangesAsync();
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
        public async Task<(bool success, string errorMessage)> UpdateCompanyDetailsAsync(CompanyDTO companyDto)
        {
            try
            {
                var (isAuthorized, userCompanyId, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyDto.Id);
                if (!isAuthorized)
                {
                    return (false, errorMessage);
                }

                // ✅ Fetch existing company using repository
                var existingCompany = await _unitOfWork.Companies.GetByIdStringAsync(companyDto.Id);

                if (existingCompany == null)
                {
                    return (false, "Company not found.");
                }

                if (!isSuperAdmin)
                {
                    // Prevent Admins from modifying restricted fields
                    existingCompany.CompanyName = companyDto.CompanyName;
                    existingCompany.Description = companyDto.Description;
                    existingCompany.IndustryType = companyDto.IndustryType;
                    existingCompany.WebsiteUrl = companyDto.WebsiteUrl;
                }
                else
                {
                    _mapper.Map(companyDto, existingCompany);
                }

                // ✅ Update the company using UnitOfWork
                await _unitOfWork.Companies.UpdateEntityAsync(existingCompany);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Company {companyDto.CompanyName} details updated successfully.");
                return (true, "Company details updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company details");
                return (false, "An error occurred while updating company details.");
            }
        }

        // Company Emails
        public async Task<(bool success, string errorMessage)> UpdateCompanyEmailsAsync(List<CompanyEmailDTO> companyEmailsDto)
        {
            try
            {
                if (companyEmailsDto == null || !companyEmailsDto.Any())
                {
                    return (false, "No emails provided for update.");
                }

                var companyId = companyEmailsDto.First().CompanyId;
                if (string.IsNullOrEmpty(companyId))
                {
                    return (false, "Invalid CompanyId provided.");
                }
                var (isAuthorized, userCompanyId, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyId);
                if (!isAuthorized)
                {
                    return (false, errorMessage);
                }

                // ✅ Fetch existing company using repository
                var existingCompany = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
                if (existingCompany == null)
                {
                    return (false, "Company not found.");
                }

                // ✅ Map DTOs to Entity
                var companyEmails = _mapper.Map<List<CompanyEmail>>(companyEmailsDto);

                // ✅ Update emails using UnitOfWork
                await _unitOfWork.CompanyEmails.UpdateEmailsAsync(companyId, companyEmails);
                await _unitOfWork.SaveChangesAsync();

                //await using var dbContext = _contextFactory.CreateDbContext();

                //var existingCompany = await dbContext.Companies
                //    .Include(c => c.CompanyEmails)
                //    .FirstOrDefaultAsync(c => c.Id == company.Id);

                //if (existingCompany == null)
                //{
                //    return (false, "Company not found.");
                //}

                //existingCompany.CompanyEmails = company.CompanyEmails;

                //await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Company emails updated for Company id: {companyId}.");
                return (true, "Company emails updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company emails");
                return (false, "An error occurred while updating company emails.");
            }
        }

        // Company Phone Numbers
        public async Task<(bool success, string errorMessage)> UpdateCompanyPhoneNumbersAsync(List<CompanyPhoneNumberDTO> companypPoneNumberDto)
        {
            try
            {
                if (companypPoneNumberDto == null || !companypPoneNumberDto.Any())
                {
                    return (false, "No phone numbers provided for update.");
                }

                var companyId = companypPoneNumberDto.First().CompanyId;
                if (string.IsNullOrEmpty(companyId))
                {
                    return (false, "Invalid CompanyId provided.");
                }
                var (isAuthorized, userCompanyId, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyId);
                if (!isAuthorized)
                {
                    return (false, errorMessage);
                }

                //await using var dbContext = _contextFactory.CreateDbContext();

                //var existingCompany = await dbContext.Companies
                //    .Include(c => c.CompanyPhoneNumbers)
                //    .FirstOrDefaultAsync(c => c.Id == company.Id);

                //if (existingCompany == null)
                //{
                //    return (false, "Company not found.");
                //}

                //existingCompany.CompanyPhoneNumbers = company.CompanyPhoneNumbers;

                //await dbContext.SaveChangesAsync();

                // ✅ Fetch existing company using repository
                var existingCompany = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
                if (existingCompany == null)
                {
                    return (false, "Company not found.");
                }

                // ✅ Map DTOs to Entity
                var companyPhoneNumbers = _mapper.Map<List<CompanyPhoneNumber>>(companypPoneNumberDto);

                // ✅ Update phone numbers using UnitOfWork
                await _unitOfWork.CompanyPhoneNumbers.UpdatePhoneNumbersAsync(companyId, companyPhoneNumbers);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Company phone numbers updated for Company id: {companyId}.");
                return (true, "Company phone numbers updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company phone numbers");
                return (false, "An error occurred while updating company phone numbers.");
            }
        }

        // To be changed to GetAllCompanyMetaData (display entire json of file used to create the index)
        public async Task<(bool success, string metadata, string errorMessage)> GetCompanyMetadataAsync(string companyId)
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

        public async Task<(bool success, string errorMessage)> GenerateAndUploadCompanyMetadataAsync(string companyId)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyId);
                if (!isAuthorized)
                {
                    return (false, errorMessage);
                }

                // ✅ Fetch company using UnitOfWork
                var company = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
                if (company == null)
                {
                    return (false, "Company not found.");
                }

                // ✅ Map to DTO for processing
                var companyDto = _mapper.Map<CompanyDTO>(company);

                // ✅ Retrieve existing metadata records from the repository
                var existingFiles = await _unitOfWork.ProcessedPretrainData.GetProcessedFilesByCompanyIdAndTypesAsync(
                    companyId, new List<FileType>
                    {
                        FileType.CompanyEmails,
                        FileType.CompanyDetails,
                        FileType.CompanyPhoneNumbers
                    });

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
                    _unitOfWork.ProcessedPretrainData.RemoveProcessedFiles(existingFiles);
                    await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine("🗑 Old metadata records deleted from database.");
                }

                Console.WriteLine($"🚀 Uploading new metadata files for Company ID {companyId}...");


                // 🔹 Generate structured metadata documents and JSON contents
                var (processedFilesDTO, jsonContents) = CompanyDataHelper.GenerateStructuredCompanyMetadata(companyDto);

                if (processedFilesDTO.Count != jsonContents.Count)
                {
                    return (false, "Mismatch between processed metadata files and JSON contents.");
                }

                // `CompanyRAGData/` folder exists in blob storage
                string baseFolderPath = $"CompanyRAGData/";

                // 🔹 Upload JSON contents to Azure Blob Storage and link them to database entries
                for (int i = 0; i < processedFilesDTO.Count; i++)
                {
                    var processedFile = processedFilesDTO[i];
                    var jsonContent = jsonContents[i];

                    using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
                    string jsonFileName = $"{baseFolderPath}company_{companyId}_pretrain_{processedFile.Id}.json";
                    string jsonFileUrl = await _blobStorageService.UploadFileAsync(jsonStream, jsonFileName, "application/json", companyId);

                    // 🔹 Step 3: Update `FileUrl` after uploading to Azure Blob Storage
                    processedFile.FileUrl = jsonFileUrl;
                }

                var processedFiles = _mapper.Map<List<ProcessedPretrainData>>(processedFilesDTO);
                // 🔹 Step 4: Store metadata in the database

                await _unitOfWork.ProcessedPretrainData.AddProcessedFilesAsync(processedFiles);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Company metadata structured and uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating and uploading metadata for company {CompanyId}", companyId);
                return (false, "An error occurred while generating metadata.");
            }
        }

        public async Task<(bool success, string errorMessage)> CreateAzureAiSearchIndexAndUploadFilesAsync(string companyId)
        {
            try
            {
                //await using var dbContext = _contextFactory.CreateDbContext();

                //// 🔹 Retrieve all processed pretrain data for the company
                //var processedFiles = await dbContext.ProcessedPretrainData
                //    .Where(f => f.CompanyId == companyId)
                //    .ToListAsync();

                // ✅ Fetch processed pretrain data using UnitOfWork
                var processedFiles = await _unitOfWork.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId);

                if (!processedFiles.Any())
                {
                    return (false, "No processed data found for this company.");
                }                

                // Create the index dynamically
                await _azureSearchService.CreateCompanyIndexAsync(companyId);


                var processedFilesDTO = _mapper.Map<List<ProcessedPretrainDataDTO>>(processedFiles);
                // ✅ Upload structured documents to the index
                await _azureSearchService.UploadDocumentsToIndexAsync(companyId, processedFilesDTO);

                return (true, "Index created and populated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating index for company {CompanyId}", companyId);
                return (false, "An error occurred while creating the index.");
            }
        }

        public async Task<(bool success, string errorMessage)> DeleteCompanyMetadataAsync(string companyId)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, errorMessage) = await CanUserEditCompanyAsync(companyId);
                if (!isAuthorized)
                {
                    return (false, errorMessage);
                }

                //await using var dbContext = _contextFactory.CreateDbContext();

                //// 🔹 Find all metadata files related to the company
                //var existingMetadataFiles = await dbContext.ProcessedPretrainData
                //    .Where(f => f.CompanyId == companyId)
                //    .ToListAsync();

                // ✅ Fetch metadata files using UnitOfWork
                var existingMetadataFiles = await _unitOfWork.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId);

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
                    _unitOfWork.ProcessedPretrainData.RemoveProcessedFiles(successfullyDeletedFiles);
                    await _unitOfWork.SaveChangesAsync();
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
        public async Task<(bool success, List<ProcessedPretrainDataDTO> files, string errorMessage)> GetCompanyPretrainingFilesAsync(string companyId)
        {
            try
            {
                // ✅ Fetch processed pretraining files using UnitOfWork
                var files = await _unitOfWork.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId);

                if (!files.Any())
                {
                    return (false, new List<ProcessedPretrainDataDTO>(), "No pretraining files found for this company.");
                }

                // ✅ Map database models to DTOs
                var filesDto = _mapper.Map<List<ProcessedPretrainDataDTO>>(files);

                return (true, filesDto, "Files retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pretraining files for company {CompanyId}", companyId);
                return (false, new List<ProcessedPretrainDataDTO>(), "An error occurred while retrieving files.");
            }
        }

        // Upload Multiple Files
        public async Task<(bool success, string errorMessage)> UploadCompanyFilesAsync(List<PretrainDataFileDTO> files)
        {
            var firstFile = files.FirstOrDefault();
            string companyId = firstFile?.CompanyId ?? string.Empty;
            try
            {
                //await using var dbContext = _contextFactory.CreateDbContext();
                //var company = await dbContext.Companies.FindAsync(companyId);

                // ✅ Ensure company exists
                var company = await _unitOfWork.Companies.GetByIdStringAsync(companyId);

                if (company == null)
                {
                    return (false, "Company not found.");
                }

                // **Define base folder path**
                string baseFolderPath = $"CompanyRAGData/";

                //// 🔹 **Step 1: Find and delete existing files of the specified types**
                //var existingFiles = await dbContext.ProcessedPretrainData
                //    .Where(f => f.CompanyId == companyId &&
                //                (f.FileType == FileType.CsvFile ||
                //                 f.FileType == FileType.ExcelFile ||
                //                 f.FileType == FileType.FAQFile ||
                //                 f.FileType == FileType.Other))
                //    .ToListAsync();

                // ✅ **Step 1: Remove existing files**
                var existingFiles = await _unitOfWork.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId);

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

                    _unitOfWork.ProcessedPretrainData.RemoveProcessedFiles(existingFiles);
                    await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine("🗑 Old files deleted from database.");
                }

                Console.WriteLine($"🚀 Uploading new files for Company ID {companyId}...");


                // 🔹 Step 1: Process files and extract structured documents
                var (processedFilesDTO, jsonContents) = await CompanyDataHelper.ProcessUploadedFilesAsync(files, _documentProcessingService);

                var processedFiles = _mapper.Map<List<ProcessedPretrainData>>(processedFilesDTO);

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

                // ✅ **Step 5: Store metadata in the database**
                await _unitOfWork.ProcessedPretrainData.AddProcessedFilesAsync(processedFiles);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Files uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading pretraining files for company {CompanyId}", companyId);
                return (false, "An error occurred during file upload.");
            }
        }

        public async Task<bool> DeleteCompanyFileAsync(ProcessedPretrainDataDTO file)
        {
            try
            {
                //await using var dbContext = _contextFactory.CreateDbContext();

                //var fileRecord = await dbContext.ProcessedPretrainData
                //    .FirstOrDefaultAsync(f => f.CompanyId == companyId && f.FileUrl == fileUrl);

                // ✅ Fetch the file record using the repository
                var fileRecord = await _unitOfWork.ProcessedPretrainData.GetByIdStringAsync(file.Id);

                if (fileRecord == null)
                {
                    return false; // File not found in DB
                }

                // Step 1: Delete from Azure Blob Storage
                bool deletedFromStorage = await _blobStorageService.DeleteFileAsync(file.FileUrl);

                if (deletedFromStorage)
                {
                    // ✅ Step 2: Remove DB entry using UnitOfWork
                    await _unitOfWork.ProcessedPretrainData.RemoveEntityAsync(fileRecord);
                    await _unitOfWork.SaveChangesAsync();
                }

                return deletedFromStorage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file for company {CompanyId}", file.CompanyId);
                return false;
            }
        }

        private async Task<(bool isAuthorized, string? userCompanyId, bool isSuperAdmin, string errorMessage)> CanUserEditCompanyAsync(string companyId)
        {
            // ✅ Get the user ID from the ClaimsPrincipal
            var userId = _currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return (false, null, false, "User is not authenticated.");
            }

            // ✅ Fetch user roles using repository
            var userRoles = _currentUser.FindFirstValue(ClaimTypes.Role);

            var isSuperAdmin = userRoles.Contains("SuperAdmin");

            if (isSuperAdmin)
            {
                return (true, null, true, string.Empty); // SuperAdmins can modify all companies
            }

            // ✅ Fetch the user's company ID
            var userCompanyId = _currentUser.FindFirstValue("CompanyId");

            if (userCompanyId == null || companyId != userCompanyId)
            {
                return (false, userCompanyId, false, "You are not authorized to update this company.");
            }

            return (true, userCompanyId, false, string.Empty);
        }
    }
}
