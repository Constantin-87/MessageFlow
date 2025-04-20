using MessageFlow.Client.Models.DTOs;

namespace MessageFlow.Client.Models
{
    public class JWTResponseModel
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public ApplicationUserDTO User { get; set; }
    }
}
