
namespace MessageFlow.Shared.DTOs
{
    public class TeamDTO
    {
        public string Id { get; set; }  // Primary key

        public string TeamName { get; set; }  // Name of the team

        public string TeamDescription { get; set; }

        // Foreign key for Company
        public string CompanyId { get; set; }

        // Many-to-many relationship with Users (through UserTeam)
        //public ICollection<UserTeamDTO> UserTeamsDTO { get; set; }  // Navigation property

        // Navigation properties
        public ICollection<ApplicationUserDTO> UsersDTO { get; set; } = new List<ApplicationUserDTO>();

    }
}
