//using MessageFlow.Shared.DTOs;
//using MessageFlow.Server.Components.Accounts.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace MessageFlow.Server.Controllers
//{
//    [Route("api/user-management")]
//    [ApiController]
//    [Authorize(Roles = "Admin,SuperAdmin")]
//    public class UserManagementController : ControllerBase
//    {
//        private readonly IUserManagementService _userManagementService;

//        public UserManagementController(IUserManagementService userManagementService)
//        {
//            _userManagementService = userManagementService;
//        }

//        // 📌 Get all users
//        [HttpGet("users")]
//        public async Task<ActionResult<List<ApplicationUserDTO>>> GetUsers()
//        {
//            var users = await _userManagementService.GetUsersAsync();
//            return Ok(users);
//        }

//        // 📌 Get user by ID
//        [HttpGet("user/{userId}")]
//        public async Task<ActionResult<ApplicationUserDTO>> GetUserById(string userId)
//        {
//            var user = await _userManagementService.GetUserByIdAsync(userId);
//            if (user == null)
//                return NotFound("User not found.");
//            return Ok(user);
//        }

//        // 📌 Create user
//        [HttpPost("create")]
//        public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request)
//        {
//            var result = await _userManagementService.CreateUserAsync(request.User, request.Password);
//            if (!result.success)
//                return BadRequest(result.errorMessage);
//            return Ok(new { message = "User created successfully" });
//        }

//        // 📌 Update user
//        [HttpPut("update/{userId}")]
//        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UserUpdateRequest request)
//        {
//            var result = await _userManagementService.UpdateUserAsync(request.User, request.NewPassword);
//            if (!result.success)
//                return BadRequest(result.errorMessage);
//            return Ok(new { message = "User updated successfully" });
//        }

//        // 📌 Delete user
//        [HttpDelete("delete/{userId}")]
//        public async Task<IActionResult> DeleteUser(string userId)
//        {
//            var success = await _userManagementService.DeleteUserAsync(userId);
//            if (!success)
//                return BadRequest("Failed to delete user.");
//            return Ok(new { message = "User deleted successfully" });
//        }

//        // 📌 Get available roles
//        [HttpGet("roles")]
//        public async Task<ActionResult<List<string>>> GetAvailableRoles()
//        {
//            var roles = await _userManagementService.GetAvailableRolesAsync();
//            return Ok(roles);
//        }
//    }

//    // 📌 DTOs for request payloads
//    public class UserCreateRequest
//    {
//        public ApplicationUserDTO User { get; set; }
//        public string Password { get; set; }
//    }

//    public class UserUpdateRequest
//    {
//        public ApplicationUserDTO User { get; set; }
//        public string? NewPassword { get; set; }
//    }
//}
