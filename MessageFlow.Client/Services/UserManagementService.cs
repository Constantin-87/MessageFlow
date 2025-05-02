using System.Net.Http.Json;
using MessageFlow.Client.Models.DTOs;
using MessageFlow.Client.Models.ViewModels;

namespace MessageFlow.Client.Services
{
    public class UserManagementService
    {
        private readonly HttpClient _httpClient;

        public UserManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ServerAPI");
        }

        public async Task<List<ApplicationUserDTO>> GetUsersAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/user-management/users");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to get users. Status: {response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<List<ApplicationUserDTO>>();
        }

        public async Task<ApplicationUserDTO?> GetUserByIdAsync(string userId)
        {
            return await _httpClient.GetFromJsonAsync<ApplicationUserDTO>($"api/user-management/user/{userId}");
        }

        public async Task<ApiNotificationResultVM> CreateUserAsync(ApplicationUserDTO user)
        {
            user.CompanyDTO = null;
            var response = await _httpClient.PostAsJsonAsync("api/user-management/create", user);
            return await ApiNotificationResultVM.FromHttpResponseAsync(response, "User created successfully.");
        }

        public async Task<ApiNotificationResultVM> UpdateUserAsync(ApplicationUserDTO user)
        {
            user.CompanyDTO = null;
            var response = await _httpClient.PutAsJsonAsync($"api/user-management/update/{user.Id}", user);
            return await ApiNotificationResultVM.FromHttpResponseAsync(response, "User updated successfully.");
        }

        public async Task<ApiNotificationResultVM> DeleteUserAsync(string userId)
        {
            var response = await _httpClient.DeleteAsync($"api/user-management/delete/{userId}");
            return await ApiNotificationResultVM.FromHttpResponseAsync(response, "User deleted successfully.");
        }

        public async Task<List<string>> GetAvailableRolesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<string>>("api/user-management/roles");
        }

        // Get all users for a company
        public async Task<List<ApplicationUserDTO>> GetUsersForCompanyAsync(string companyId)
        {
            return await _httpClient.GetFromJsonAsync<List<ApplicationUserDTO>>($"api/user-management/{companyId}") ?? new List<ApplicationUserDTO>();
        }
    }
}