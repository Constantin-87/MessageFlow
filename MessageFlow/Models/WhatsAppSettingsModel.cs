namespace MessageFlow.Models
{
    public class WhatsAppSettingsModel
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string BusinessAccountId { get; set; } = string.Empty;
        public string WebhookVerifyToken { get; set; } = string.Empty;
        public List<PhoneNumberInfo> PhoneNumbers { get; set; } = new();
    }   
}
