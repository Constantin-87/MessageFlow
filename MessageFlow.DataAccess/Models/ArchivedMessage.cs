namespace MessageFlow.DataAccess.Models
{
    public class ArchivedMessage
    {
        public string Id { get; set; } = string.Empty;
        public string ArchivedConversationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}