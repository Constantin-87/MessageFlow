using MessageFlow.Client.Models.DTOs;
using MessageFlow.Client.Models.ViewModels;
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

        public async Task<ApiNotificationResultVM> AddTeamToCompanyAsync(string companyId, string teamName, string teamDescription, List<ApplicationUserDTO> assignedUsers)
        {
            var teamDto = new TeamDTO
            {
                CompanyId = companyId,
                TeamName = teamName,
                TeamDescription = teamDescription,
                AssignedUsersDTO = assignedUsers
            };
            var response = await _httpClient.PostAsJsonAsync("api/TeamsManagement", teamDto);
            var message = await response.Content.ReadAsStringAsync();
            return new ApiNotificationResultVM
            {
                IsSuccess = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Team created successfully." : "Error while creating the Team, contact administrator for support."
            };
        }

        public async Task<ApiNotificationResultVM> UpdateTeamAsync(TeamDTO team)
        {
            var response = await _httpClient.PutAsJsonAsync("api/TeamsManagement", team);
            var message = await response.Content.ReadAsStringAsync();
            return new ApiNotificationResultVM
            {
                IsSuccess = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Team updated successfully" : "Error while creating the Team, contact administrator for support."
            };
        }

        public async Task<ApiNotificationResultVM> DeleteTeamByIdAsync(string teamId)
        {
            var response = await _httpClient.DeleteAsync($"api/TeamsManagement/{teamId}");
            var message = await response.Content.ReadAsStringAsync();
            return new ApiNotificationResultVM
            {
                IsSuccess = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Team deleted successfully" : "Error while creating the Team, contact administrator for support."
            };
        }
    }
}