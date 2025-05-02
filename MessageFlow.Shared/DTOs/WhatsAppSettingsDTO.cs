namespace MessageFlow.Shared.DTOs
{
    public class WhatsAppSettingsDTO
    {
        public string Id { get; set; }
        public string CompanyId { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string BusinessAccountId { get; set; } = string.Empty;
        public List<PhoneNumberInfoDTO> PhoneNumbers { get; set; } = new();
    }   
}