using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Shared.DTOs
{
    public class CompanyPhoneNumberDTO
    {
        public int Id { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public string Description { get; set; }

        public int CompanyId { get; set; }
        public CompanyDTO Company { get; set; }
    }
}
