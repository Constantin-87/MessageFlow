using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MessageFlow.DataAccess.Models
{
    public class ApplicationUser : IdentityUser
    {

        [Required(ErrorMessage = "Please select a company.")]
        public string CompanyId { get; set; }

        // Navigation property for many-to-many relationship
        public ICollection<Team> Teams { get; set; }

        // Navigation property for the related Company
        public Company Company { get; set; }

        // Activity timestamp
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}



