
using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Shared.DTOs
{
    public class ApplicationUserDTO
    {
        public string Id { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }
        public string PhoneNumber { get; set; }        

        [Required(ErrorMessage = "Please select a company.")]
        public string CompanyId { get; set; }
        [Required(ErrorMessage = "Please select a role.")]
        public string Role { get; set; }
        public bool LockoutEnabled { get; set; }

        // Navigation property for many-to-many relationship
        public ICollection<TeamDTO> TeamsDTO { get; set; }

        // Navigation property for the related Company
        public CompanyDTO CompanyDTO { get; set; }

        // Activity timestamp
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
}
