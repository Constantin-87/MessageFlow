using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Client.Models.DTOs
{
    public class CompanyEmailDTO
    {
        public string Id { get; set; }

        [Required, EmailAddress]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "Email description required.")]
        public string Description { get; set; }

        public string CompanyId { get; set; }
    }
}
