namespace MessageFlow.Models
{
    public class ArchivedMessage
    {
        public string Id { get; set; } = string.Empty;
        public string ArchivedConversationId { get; set; } // Foreign key to Conversation
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }

        // Navigation property
        public ArchivedConversation ArchivedConversation { get; set; }
    }
}
