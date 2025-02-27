
using MessageFlow.Shared.DTOs;

namespace MessageFlow.DataAccess.Models
{
    public class Team
    {
        public string Id { get; set; }  // Primary key

        public string TeamName { get; set; }  // Name of the team

        public string TeamDescription { get; set; }

        // Foreign key for Company
        public string CompanyId { get; set; }

        // Navigation property for the related Company
        public Company Company { get; set; }

        // Navigation properties
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}
