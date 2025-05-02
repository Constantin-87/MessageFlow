using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Client.Models.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Username is required.")]
        public string? Username { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string? Password { get; set; }
    }
}