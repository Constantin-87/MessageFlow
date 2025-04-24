using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Client.Models.DTOs
{
    public class PhoneNumberInfoDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number ID is required")]
        public string PhoneNumberId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number Description is required")]
        public string PhoneNumberDesc { get; set; } = string.Empty;

        public string CompanyId { get; set; } = string.Empty;
        public string WhatsAppSettingsId { get; set; } = string.Empty;
    }
}