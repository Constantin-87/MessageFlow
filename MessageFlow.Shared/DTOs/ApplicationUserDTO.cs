
using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Shared.DTOs
{
    public class ApplicationUserDTO
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "The UserName field is required.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "The UserEmail field is required.")]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "The PhoneNumber field is required.")]
        public string PhoneNumber { get; set; }

        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a company.")]
        public string CompanyId { get; set; }

        [Required(ErrorMessage = "Please select a role.")]
        public string Role { get; set; }

        public bool LockoutEnabled { get; set; }

        public List<string> TeamIds { get; set; } = new();

        // Navigation property for many-to-many relationship
        //public ICollection<TeamDTO>? TeamsDTO { get; set; } = null;

        // Navigation property for the related Company
        public CompanyDTO? CompanyDTO { get; set; } = null;

        // Activity timestamp
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
}
