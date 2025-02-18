using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Please select a company.")]
        public int CompanyId { get; set; }

        // Navigation property for many-to-many relationship
        public ICollection<UserTeam> UserTeams { get; set; }

        // Navigation property for the related Company
        public Company Company { get; set; }

        // Activity timestamp
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
}



