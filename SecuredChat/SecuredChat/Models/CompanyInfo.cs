namespace SecuredChat.Models
{
    public class CompanyInfo
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;  // Ensure non-null default values
        public string CompanyName { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
    }
}
