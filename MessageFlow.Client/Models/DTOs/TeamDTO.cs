namespace MessageFlow.Client.Models.DTOs
{
    public class TeamDTO
    {
        public string? Id { get; set; }

        public string TeamName { get; set; }

        public string TeamDescription { get; set; }

        public string CompanyId { get; set; }

        public ICollection<ApplicationUserDTO> AssignedUsersDTO { get; set; } = new List<ApplicationUserDTO>();

    }
}
