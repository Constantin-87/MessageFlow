using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Please select a company.")]
        public int CompanyId { get; set; }

        // Navigation property for many-to-many relationship
        public ICollection<UserTeam> UserTeams { get; set; }
    }
}



