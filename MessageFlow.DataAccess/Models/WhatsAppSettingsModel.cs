using System.ComponentModel.DataAnnotations;

namespace MessageFlow.DataAccess.Models
{
    public class WhatsAppSettingsModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "CompanyId is required.")]
        public string CompanyId { get; set; }

        [Required(ErrorMessage = "AccessToken is required.")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "BusinessAccountId is required.")]
        public string BusinessAccountId { get; set; } = string.Empty;

        public List<PhoneNumberInfo> PhoneNumbers { get; set; } = new();
    }
}