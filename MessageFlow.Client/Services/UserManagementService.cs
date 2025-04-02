using System.Net.Http.Json;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Client.Services
{
    public class UserManagementService
    {
        private readonly HttpClient _httpClient;

        public UserManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ServerAPI"); // ✅ Use Server API
        }

        public async Task<List<ApplicationUserDTO>> GetUsersAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/user-management/users");
            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("User is not authorized to access this resource.");
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get users. Status: {response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<List<ApplicationUserDTO>>();
        }

        public async Task<ApplicationUserDTO?> GetUserByIdAsync(string userId)
        {
            return await _httpClient.GetFromJsonAsync<ApplicationUserDTO>($"api/user-management/user/{userId}");
        }

        public async Task<(bool success, string message)> CreateUserAsync(ApplicationUserDTO user)
        {
            user.CompanyDTO = null; // ✅ Ignore it during create/update calls
            var response = await _httpClient.PostAsJsonAsync("api/user-management/create", user);
            return response.IsSuccessStatusCode ? (true, "User created") : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool success, string message)> UpdateUserAsync(ApplicationUserDTO user)
        {
            user.CompanyDTO = null; // ✅ Ignore it during create/update calls
            var response = await _httpClient.PutAsJsonAsync($"api/user-management/update/{user.Id}", user);
            return response.IsSuccessStatusCode ? (true, "User updated") : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var response = await _httpClient.DeleteAsync($"api/user-management/delete/{userId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<string>> GetAvailableRolesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<string>>("api/user-management/roles");
        }

        // ✅ Get all users for a company
        public async Task<List<ApplicationUserDTO>> GetUsersForCompanyAsync(string companyId)
        {
            return await _httpClient.GetFromJsonAsync<List<ApplicationUserDTO>>($"api/user-management/{companyId}") ?? new List<ApplicationUserDTO>();
        }
    }

}
