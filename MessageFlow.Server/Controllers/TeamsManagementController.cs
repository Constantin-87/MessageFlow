using MessageFlow.Server.Components.Accounts.Services;
using MessageFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessageFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class TeamsManagementController : ControllerBase
    {
        private readonly TeamsManagementService _teamsManagementService;

        public TeamsManagementController(TeamsManagementService teamsManagementService)
        {
            _teamsManagementService = teamsManagementService;
        }

        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetTeamsForCompany(string companyId)
        {
            var teams = await _teamsManagementService.GetTeamsForCompanyAsync(companyId);
            return Ok(teams);
        }

        [HttpGet("{teamId}")]
        public async Task<IActionResult> GetUsersForTeam(string teamId)
        {
            var users = await _teamsManagementService.GetUsersForTeamAsync(teamId);
            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeam([FromBody] TeamDTO teamDto)
        {
            var (success, errorMessage) = await _teamsManagementService.AddTeamToCompanyAsync(teamDto);
            return success ? Ok("Team created successfully") : BadRequest(errorMessage);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTeam([FromBody] TeamDTO teamDto)
        {
            var (success, errorMessage) = await _teamsManagementService.UpdateTeamAsync(teamDto);
            return success ? Ok("Team updated successfully") : BadRequest(errorMessage);
        }

        [HttpDelete("{teamId}")]
        public async Task<IActionResult> DeleteTeam(string teamId)
        {
            var (success, errorMessage) = await _teamsManagementService.DeleteTeamByIdAsync(teamId);
            return success ? Ok("Team deleted successfully") : BadRequest(errorMessage);
        }
    }
}
