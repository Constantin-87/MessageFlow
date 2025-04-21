using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Client.Models.DTOs
{
    public class CompanyPhoneNumberDTO
    {
        public string Id { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public string Description { get; set; }

        public string CompanyId { get; set; }
    }
}
