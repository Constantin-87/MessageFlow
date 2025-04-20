using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Client.Models.DTOs
{
    public class WhatsAppCoreSettingsDTO
    {
        [Required(ErrorMessage = "Company ID is required.")]
        public string CompanyId { get; set; }

        [Required(ErrorMessage = "Business Account ID is required.")]
        public string BusinessAccountId { get; set; }

        [Required(ErrorMessage = "Access Token is required.")]
        public string AccessToken { get; set; }
    }
}
