using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Models
{
    public class CompanyPhoneNumber
    {
        public int Id { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public string Description { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; }
    }
}
