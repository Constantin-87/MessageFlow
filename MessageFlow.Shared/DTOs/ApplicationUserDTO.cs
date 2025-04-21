
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MessageFlow.Shared.DTOs
{
    public class ApplicationUserDTO
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "The UserName field is required.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "The UserEmail field is required.")]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "The PhoneNumber field is required.")]
        public string PhoneNumber { get; set; }

        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a company.")]
        public string CompanyId { get; set; }

        public string? Role { get; set; }

        public bool LockoutEnabled { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<TeamDTO>? TeamsDTO { get; set; } = null;

        public CompanyDTO? CompanyDTO { get; set; } = null;
    }
}