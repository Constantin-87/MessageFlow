
namespace MessageFlow.Shared.DTOs
{
    public class TeamDTO
    {
        public int Id { get; set; }  // Primary key

        public string TeamName { get; set; }  // Name of the team

        public string TeamDescription { get; set; }

        // Foreign key for Company
        public int CompanyId { get; set; }

        //// Navigation property for the related Company
        //public CompanyDTO Company { get; set; }

        //// Many-to-many relationship with Users (through UserTeam)
        //public ICollection<UserTeam> UserTeams { get; set; }  // Navigation property
    }
}
