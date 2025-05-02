namespace MessageFlow.Client.Models.DTOs
{
    public class LoginResultDTO
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string ErrorMessage { get; set; }
        public ApplicationUserDTO? User { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
}