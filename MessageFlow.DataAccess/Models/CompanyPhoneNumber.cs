using System.ComponentModel.DataAnnotations;

namespace MessageFlow.DataAccess.Models
{
    public class CompanyPhoneNumber
    {
        public string Id { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public string Description { get; set; }

        public string CompanyId { get; set; }
    }
}
