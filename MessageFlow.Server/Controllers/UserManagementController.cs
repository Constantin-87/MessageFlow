using MessageFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Server.MediatorComponents.UserManagement.Queries;
using MessageFlow.Server.MediatorComponents.UserManagement.Commands;

namespace MessageFlow.Server.Controllers
{
    [Route("api/user-management")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UserManagementController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserManagementController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Get all users
        [HttpGet("users")]
        public async Task<ActionResult<List<ApplicationUserDTO>>> GetUsers()
        {
            var users = await _mediator.Send(new GetAllUsersQuery());
            return Ok(users);
        }

        // Get user by ID
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApplicationUserDTO>> GetUserById(string userId)
        {
            var user = await _mediator.Send(new GetUserByIdQuery(userId));
            if (user == null)
                return NotFound("User not found.");
            return Ok(user);
        }

        // Create user
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] ApplicationUserDTO request)
        {
            var result = await _mediator.Send(new CreateUserCommand(request));
            if (!result.success)
                return BadRequest(result.errorMessage);
            return Ok(new { message = "User created successfully" });
        }

        // Update user
        [HttpPut("update/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] ApplicationUserDTO request)
        {
            var result = await _mediator.Send(new UpdateUserCommand(request));
            if (!result.success)
                return BadRequest(result.errorMessage);
            return Ok(new { message = "User updated successfully" });
        }

        // Delete user
        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var success = await _mediator.Send(new DeleteUserCommand(userId));
            if (!success)
                return BadRequest("Failed to delete user.");
            return Ok(new { message = "User deleted successfully" });
        }

        [HttpDelete("delete-company-users/{companyId}")]
        public async Task<IActionResult> DeleteUsersByCompanyId(string companyId)
        {
            var success = await _mediator.Send(new DeleteUsersByCompanyCommand(companyId));
            if (!success)
                return BadRequest("Failed to delete users for the specified company.");

            return Ok(new { message = "All users for this company were deleted successfully." });
        }

        // Get available roles
        [HttpGet("roles")]
        public async Task<ActionResult<List<string>>> GetAvailableRoles()
        {
            var roles = await _mediator.Send(new GetAvailableRolesQuery());
            return Ok(roles);
        }

        // Get all users for a specific company
        [HttpGet("{companyId}")]
        public async Task<ActionResult<List<ApplicationUserDTO>>> GetUsersForCompany(string companyId)
        {
            var users = await _mediator.Send(new GetUsersForCompanyQuery(companyId));

            if (users == null || !users.Any())
            {
                return NotFound("No users found for the company.");
            }

            return Ok(users);
        }

        [HttpPost("get-users-by-ids")]
        public async Task<ActionResult<List<ApplicationUserDTO>>> GetUsersByIds([FromBody] List<string> userIds)
        {
            if (userIds == null || !userIds.Any())
                return BadRequest("UserIds are required.");

            var users = await _mediator.Send(new GetUsersByIdsQuery(userIds));
            return Ok(users);
        }
    }
}
