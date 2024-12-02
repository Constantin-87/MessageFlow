namespace MessageFlow.Shared.Models
{
    public class UserTeam
    {
        // Composite key, a user can belong to many teams, and a team can have many users
        public string UserId { get; set; }  // Foreign key for ApplicationUser

        public int TeamId { get; set; }  // Foreign key for Team

        // Navigation properties
        public ApplicationUser User { get; set; }  // Navigation property for User
        public Team Team { get; set; }  // Navigation property for Team
    }
}
