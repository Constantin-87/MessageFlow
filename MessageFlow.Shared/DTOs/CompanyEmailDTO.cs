using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Shared.DTOs
{
    public class CompanyEmailDTO
    {
        public int Id { get; set; }

        [Required, EmailAddress]
        public string EmailAddress { get; set; }

        public string Description { get; set; }

        public int CompanyId { get; set; }
        public CompanyDTO Company { get; set; }
    }
}
