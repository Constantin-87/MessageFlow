using MessageFlow.Shared.DTOs;
using MessageFlow.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessageFlow.Identity.Controllers
{
    [Route("api/user-management")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;

        public UserManagementController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        // Get all users
        [HttpGet("users")]
        public async Task<ActionResult<List<ApplicationUserDTO>>> GetUsers()
        {
            var users = await _userManagementService.GetUsersAsync();
            return Ok(users);
        }

        // Get user by ID
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApplicationUserDTO>> GetUserById(string userId)
        {
            var user = await _userManagementService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound("User not found.");
            return Ok(user);
        }

        // 📌 Create user
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] ApplicationUserDTO request)
        {
            var result = await _userManagementService.CreateUserAsync(request);
            if (!result.success)
                return BadRequest(result.errorMessage);
            return Ok(new { message = "User created successfully" });
        }

        // 📌 Update user
        [HttpPut("update/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] ApplicationUserDTO request)
        {
            var result = await _userManagementService.UpdateUserAsync(request);
            if (!result.success)
                return BadRequest(result.errorMessage);
            return Ok(new { message = "User updated successfully" });
        }

        // 📌 Delete user
        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var success = await _userManagementService.DeleteUserAsync(userId);
            if (!success)
                return BadRequest("Failed to delete user.");
            return Ok(new { message = "User deleted successfully" });
        }

        [HttpDelete("delete-company-users/{companyId}")]
        public async Task<IActionResult> DeleteUsersByCompanyId(string companyId)
        {
            var success = await _userManagementService.DeleteUsersByCompanyIdAsync(companyId);
            if (!success)
                return BadRequest("Failed to delete users for the specified company.");

            return Ok(new { message = "All users for this company were deleted successfully." });
        }

        // 📌 Get available roles
        [HttpGet("roles")]
        public async Task<ActionResult<List<string>>> GetAvailableRoles()
        {
            var roles = await _userManagementService.GetAvailableRolesAsync();
            return Ok(roles);
        }

        // ✅ Get all users for a specific company
        [HttpGet("{companyId}")]
        public async Task<ActionResult<List<ApplicationUserDTO>>> GetUsersForCompany(string companyId)
        {
            var users = await _userManagementService.GetUsersForCompanyAsync(companyId);

            if (users == null || !users.Any())
            {
                return NotFound("No users found for the company.");
            }

            return Ok(users);
        }
    }
}
