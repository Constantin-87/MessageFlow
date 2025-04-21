using MessageFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using MessageFlow.Server.MediatR.TeamManagement.Queries;
using MessageFlow.Server.MediatR.TeamManagement.Commands;

namespace MessageFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class TeamsManagementController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TeamsManagementController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetTeamsForCompany(string companyId)
        {
            var teams = await _mediator.Send(new GetTeamsForCompanyQuery(companyId));
            return Ok(teams);
        }

        [HttpGet("{teamId}")]
        public async Task<IActionResult> GetUsersForTeam(string teamId)
        {
            var users = await _mediator.Send(new GetUsersForTeamQuery(teamId));
            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeam([FromBody] TeamDTO teamDto)
        {
            var (success, errorMessage) = await _mediator.Send(new AddTeamToCompanyCommand(teamDto));
            return success ? Ok("Team created successfully") : BadRequest(errorMessage);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTeam([FromBody] TeamDTO teamDto)
        {
            var (success, errorMessage) = await _mediator.Send(new UpdateTeamCommand(teamDto));
            return success ? Ok("Team updated successfully") : BadRequest(errorMessage);
        }

        [HttpDelete("{teamId}")]
        public async Task<IActionResult> DeleteTeam(string teamId)
        {
            var (success, errorMessage) = await _mediator.Send(new DeleteTeamByIdCommand(teamId));
            return success ? Ok("Team deleted successfully") : BadRequest(errorMessage);
        }
    }
}
