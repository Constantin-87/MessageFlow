using MessageFlow.Shared.DTOs;
using System.Net.Http.Json;

namespace MessageFlow.Client.Services
{
    public class TeamsManagementService
    {
        private readonly HttpClient _httpClient;

        public TeamsManagementService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TeamDTO>> GetTeamsForCompanyAsync(string companyId)
        {
            return await _httpClient.GetFromJsonAsync<List<TeamDTO>>($"api/TeamsManagement/company/{companyId}") ?? new List<TeamDTO>();
        }

        public async Task<List<ApplicationUserDTO>> GetUsersForTeamAsync(string teamId)
        {
            return await _httpClient.GetFromJsonAsync<List<ApplicationUserDTO>>($"api/TeamsManagement/{teamId}") ?? new List<ApplicationUserDTO>();
        }

        // Add a new team to a company
        public async Task<(bool success, string message)> AddTeamToCompanyAsync(string companyId, string teamName, string teamDescription, List<ApplicationUserDTO> assignedUsers)
        {
            var teamDto = new TeamDTO
            {
                CompanyId = companyId,
                TeamName = teamName,
                TeamDescription = teamDescription,
                AssignedUsersDTO = assignedUsers
            };

            var response = await _httpClient.PostAsJsonAsync("api/TeamsManagement", teamDto);
            return response.IsSuccessStatusCode
                ? (true, "Team created successfully")
                : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool success, string message)> UpdateTeamAsync(TeamDTO team)
        {
            var response = await _httpClient.PutAsJsonAsync("api/TeamsManagement", team);
            return response.IsSuccessStatusCode ? (true, "Team updated successfully") : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool success, string message)> DeleteTeamByIdAsync(string teamId)
        {
            var response = await _httpClient.DeleteAsync($"api/TeamsManagement/{teamId}");
            return response.IsSuccessStatusCode ? (true, "Team deleted successfully") : (false, await response.Content.ReadAsStringAsync());
        }
    }

}
