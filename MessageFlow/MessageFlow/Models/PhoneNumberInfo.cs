namespace MessageFlow.Models
{
    public class PhoneNumberInfo
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string PhoneNumberId { get; set; } = string.Empty;
        public string PhoneNumberDesc { get; set; } = string.Empty;

        // Foreign key to WhatsAppSettingsModel
        public int WhatsAppSettingsModelId { get; set; }
    }
}
