namespace MessageFlow.DataAccess.Models
{
    public class WhatsAppSettingsModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CompanyId { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string BusinessAccountId { get; set; } = string.Empty;
        public List<PhoneNumberInfo> PhoneNumbers { get; set; } = new();
    }   
}
