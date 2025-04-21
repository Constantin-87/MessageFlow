
namespace MessageFlow.DataAccess.Models
{
    public class Team
    {
        public string Id { get; set; }

        public string TeamName { get; set; }

        public string TeamDescription { get; set; }

        public string CompanyId { get; set; }

        // Navigation properties
        public Company Company { get; set; }

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}
