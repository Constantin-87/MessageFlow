using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MessageFlow.DataAccess.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Please select a company.")]
        public string CompanyId { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }

        // Navigation properties
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public Company Company { get; set; }
    }
}