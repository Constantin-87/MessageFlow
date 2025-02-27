using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Shared.DTOs
{
    public class CompanyEmailDTO
    {
        public string Id { get; set; }

        [Required, EmailAddress]
        public string EmailAddress { get; set; }

        public string Description { get; set; }

        public string CompanyId { get; set; }
    }
}
