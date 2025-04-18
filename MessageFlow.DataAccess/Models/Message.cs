namespace MessageFlow.DataAccess.Models
{
    public class Message
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProviderMessageId { get; set; } = string.Empty;
        public required string ConversationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Conversation Conversation { get; set; }
    }
}
