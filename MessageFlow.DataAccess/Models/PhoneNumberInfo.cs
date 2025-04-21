using System.ComponentModel.DataAnnotations;

namespace MessageFlow.DataAccess.Models
{
    public class PhoneNumberInfo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number ID is required")]
        public string PhoneNumberId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number Description is required")]
        public string PhoneNumberDesc { get; set; } = string.Empty;

        public string WhatsAppSettingsModelId { get; set; }
        public WhatsAppSettingsModel WhatsAppSettings { get; set; }
    }
}
