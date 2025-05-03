using System.Net.Http.Json;
using MessageFlow.Client.Models.DTOs;
using MessageFlow.Client.Models.ViewModels;

namespace MessageFlow.Client.Services
{    
    public class CompanyManagementService
    {
        private readonly HttpClient _httpClient;

        public CompanyManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ServerAPI");
        }

        public async Task<List<CompanyDTO>> GetAllCompaniesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/company/all");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to fetch companies. Status: {response.StatusCode}, Error: {errorMessage}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                return await response.Content.ReadFromJsonAsync<List<CompanyDTO>>() ?? new List<CompanyDTO>();
            }
            catch (Exception)
            {
                return new List<CompanyDTO>();
            }
        }

        // Get company details by ID
        public async Task<CompanyDTO?> GetCompanyByIdAsync(string companyId)
        {
            return await _httpClient.GetFromJsonAsync<CompanyDTO>($"api/company/{companyId}");
        }

        // Create a new company
        public async Task<ApiNotificationResultVM> CreateCompanyAsync(CompanyDTO companyDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/company/create", companyDto);
            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }

        // Update company details
        public async Task<ApiNotificationResultVM> UpdateCompanyAsync(CompanyDTO companyDto)
        {
            var response = await _httpClient.PutAsJsonAsync("api/company/update", companyDto);
            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }

        // Delete a company
        public async Task<ApiNotificationResultVM> DeleteCompanyAsync(string companyId)
        {
            var response = await _httpClient.DeleteAsync($"api/company/delete/{companyId}");
            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }

        // Get the logged-in user's company
        public async Task<CompanyDTO?> GetCompanyForUserAsync(string userId)
        {
            return await _httpClient.GetFromJsonAsync<CompanyDTO>("api/company/user-company");
        }        

        // Update company emails
        public async Task<ApiNotificationResultVM> UpdateCompanyEmailsAsync(List<CompanyEmailDTO> emails)
        {
            var response = await _httpClient.PutAsJsonAsync("api/company/update-emails", emails);
            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }

        // Update company phone numbers
        public async Task<ApiNotificationResultVM> UpdateCompanyPhoneNumbersAsync(List<CompanyPhoneNumberDTO> phoneNumbers)
        {
            var response = await _httpClient.PutAsJsonAsync("api/company/update-phone-numbers", phoneNumbers);
            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }

        // Fetch company metadata
        public async Task<(ApiNotificationResultVM result, string metadata)> GetCompanyMetadataAsync(string companyId)
        {
            var response = await _httpClient.GetAsync($"api/company/{companyId}/metadata");

            var result = await ApiNotificationResultVM.FromHttpResponseAsync(response, "Company metadata retrieved successfully");

            var metadata = result.IsSuccess
                ? await response.Content.ReadAsStringAsync()
                : string.Empty;

            return (result, metadata);
        }

        // Generate and upload metadata
        public async Task<ApiNotificationResultVM> GenerateAndUploadCompanyMetadataAsync(string companyId)
        {
            var response = await _httpClient.PostAsync($"api/company/{companyId}/generate-metadata", null);
            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }

        // Delete metadata
        public async Task<ApiNotificationResultVM> DeleteCompanyMetadataAsync(string companyId)
        {
            var response = await _httpClient.DeleteAsync($"api/company/{companyId}/delete-metadata");
            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }

        // Fetch pretraining files
        public async Task<(ApiNotificationResultVM result, List<ProcessedPretrainDataDTO> files)> GetCompanyPretrainingFilesAsync(string companyId)
        {
            var response = await _httpClient.GetAsync($"api/company/{companyId}/pretraining-files");

            var result = await ApiNotificationResultVM.FromHttpResponseAsync(response, "Pretraining files retrieved successfully");

            var files = result.IsSuccess
                ? await response.Content.ReadFromJsonAsync<List<ProcessedPretrainDataDTO>>() ?? new List<ProcessedPretrainDataDTO>()
                : new List<ProcessedPretrainDataDTO>();

            return (result, files);;
        }

        public async Task<ApiNotificationResultVM> UploadCompanyFilesAsync(List<PretrainDataFileDTO> files)
        {
            using var content = new MultipartFormDataContent();

            if (files.Count == 0)
                return new ApiNotificationResultVM
                {
                    IsSuccess = false,
                    Message = "No files provided."
                };

            content.Add(new StringContent(files[0].CompanyId), "companyId");

            foreach (var file in files)
            {
                var streamContent = new StreamContent(file.FileContent);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                content.Add(streamContent, "files", file.FileName);
                content.Add(new StringContent(file.FileDescription ?? ""), $"descriptions-{file.FileName}");
            }

            var response = await _httpClient.PostAsync("api/company/upload-files", content);

            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }

        // Delete a specific file
        public async Task<ApiNotificationResultVM> DeleteCompanyFileAsync(ProcessedPretrainDataDTO file)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "api/company/delete-file")
            {
                Content = JsonContent.Create(file)
            };

            var response = await _httpClient.SendAsync(request);

            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }

        // Create Azure AI Search Index
        public async Task<ApiNotificationResultVM> CreateAzureAiSearchIndexAndUploadFilesAsync(string companyId)
        {
            var response = await _httpClient.PostAsync($"api/company/{companyId}/create-search-index", null);

            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }

        public async Task<ApiNotificationResultVM> UpdateCompanyDetailsAsync(CompanyDTO companyDto)
        {
            var response = await _httpClient.PutAsJsonAsync("api/company/update", companyDto);

            return await ApiNotificationResultVM.FromHttpResponseAsync(response);
        }
    }
}