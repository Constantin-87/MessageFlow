namespace MessageFlow.Server.Models
{
    public class ArchivedConversation
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; } = string.Empty;
        public string AssignedUserId { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public ICollection<ArchivedMessage> Messages { get; set; } = new List<ArchivedMessage>();
    }
}
