
namespace SecuredChat.Models
{
    public class Team
    {
        public int Id { get; set; }  // Primary key

        public string TeamName { get; set; }  // Name of the team

        // Foreign key for Company
        public int CompanyId { get; set; }

        // Navigation property for the related Company
        public Company Company { get; set; }

        // Many-to-many relationship with Users (through UserTeam)
        public ICollection<UserTeam> UserTeams { get; set; }  // Navigation property
    }
}
