using System.Net.Http.Json;
using MessageFlow.Client.Models.DTOs;

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
            catch (Exception ex)
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
        public async Task<(bool success, string message)> CreateCompanyAsync(CompanyDTO companyDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/company/create", companyDto);
            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadAsStringAsync())
                : (false, await response.Content.ReadAsStringAsync());
        }

        // Update company details
        public async Task<(bool success, string message)> UpdateCompanyAsync(CompanyDTO companyDto)
        {
            var response = await _httpClient.PutAsJsonAsync("api/company/update", companyDto);
            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadAsStringAsync())
                : (false, await response.Content.ReadAsStringAsync());
        }

        // Delete a company
        public async Task<(bool success, string message)> DeleteCompanyAsync(string companyId)
        {
            var response = await _httpClient.DeleteAsync($"api/company/delete/{companyId}");
            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadAsStringAsync())
                : (false, await response.Content.ReadAsStringAsync());
        }

        // Get the logged-in user's company
        public async Task<CompanyDTO?> GetCompanyForUserAsync(string userId)
        {
            return await _httpClient.GetFromJsonAsync<CompanyDTO>("api/company/user-company");
        }        

        // Update company emails
        public async Task<(bool success, string message)> UpdateCompanyEmailsAsync(List<CompanyEmailDTO> emails)
        {
            var response = await _httpClient.PutAsJsonAsync("api/company/update-emails", emails);
            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadAsStringAsync())
                : (false, await response.Content.ReadAsStringAsync());
        }

        // Update company phone numbers
        public async Task<(bool success, string message)> UpdateCompanyPhoneNumbersAsync(List<CompanyPhoneNumberDTO> phoneNumbers)
        {
            var response = await _httpClient.PutAsJsonAsync("api/company/update-phone-numbers", phoneNumbers);
            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadAsStringAsync())
                : (false, await response.Content.ReadAsStringAsync());
        }

        // Fetch company metadata
        public async Task<(bool success, string metadata, string message)> GetCompanyMetadataAsync(string companyId)
        {
            var response = await _httpClient.GetAsync($"api/company/{companyId}/metadata");
            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadAsStringAsync(), "Success")
                : (false, string.Empty, await response.Content.ReadAsStringAsync());
        }

        // Generate and upload metadata
        public async Task<(bool success, string message)> GenerateAndUploadCompanyMetadataAsync(string companyId)
        {
            var response = await _httpClient.PostAsync($"api/company/{companyId}/generate-metadata", null);
            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadAsStringAsync())
                : (false, await response.Content.ReadAsStringAsync());
        }

        // Delete metadata
        public async Task<(bool success, string message)> DeleteCompanyMetadataAsync(string companyId)
        {
            var response = await _httpClient.DeleteAsync($"api/company/{companyId}/delete-metadata");
            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadAsStringAsync())
                : (false, await response.Content.ReadAsStringAsync());
        }

        // Fetch pretraining files
        public async Task<(bool success, List<ProcessedPretrainDataDTO> files, string message)> GetCompanyPretrainingFilesAsync(string companyId)
        {
            var response = await _httpClient.GetAsync($"api/company/{companyId}/pretraining-files");
            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadFromJsonAsync<List<ProcessedPretrainDataDTO>>() ?? new List<ProcessedPretrainDataDTO>(), "Success")
                : (false, new List<ProcessedPretrainDataDTO>(), await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool success, string message)> UploadCompanyFilesAsync(List<PretrainDataFileDTO> files)
        {
            using var content = new MultipartFormDataContent();

            if (files.Count == 0)
                return (false, "No files provided.");

            content.Add(new StringContent(files[0].CompanyId), "companyId");

            foreach (var file in files)
            {
                var streamContent = new StreamContent(file.FileContent);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                content.Add(streamContent, "files", file.FileName);
                content.Add(new StringContent(file.FileDescription ?? ""), $"descriptions-{file.FileName}");
            }

            var response = await _httpClient.PostAsync("api/company/upload-files", content);

            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadAsStringAsync())
                : (false, await response.Content.ReadAsStringAsync());
        }

        // Delete a specific file
        public async Task<bool> DeleteCompanyFileAsync(ProcessedPretrainDataDTO file)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "api/company/delete-file")
            {
                Content = JsonContent.Create(file)
            };

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        // Create Azure AI Search Index
        public async Task<(bool success, string message)> CreateAzureAiSearchIndexAndUploadFilesAsync(string companyId)
        {
            var response = await _httpClient.PostAsync($"api/company/{companyId}/create-search-index", null);
            return response.IsSuccessStatusCode
                ? (true, await response.Content.ReadAsStringAsync())
                : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool success, string message)> UpdateCompanyDetailsAsync(CompanyDTO companyDto)
        {
            var response = await _httpClient.PutAsJsonAsync("api/company/update", companyDto);
            return response.IsSuccessStatusCode
                ? (true, "Company details updated successfully")
                : (false, await response.Content.ReadAsStringAsync());
        }
    }
}