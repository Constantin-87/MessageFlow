namespace MessageFlow.Shared.DTOs
{
    public class TeamDTO
    {
        public string? Id { get; set; }

        public string TeamName { get; set; }

        public string TeamDescription { get; set; }

        // Foreign key for Company
        public string CompanyId { get; set; }

        // Navigation properties
        public ICollection<ApplicationUserDTO> AssignedUsersDTO { get; set; } = new List<ApplicationUserDTO>();

    }
}
