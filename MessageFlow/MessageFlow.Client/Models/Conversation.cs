﻿namespace MessageFlow.Client.Models
{
    public class Conversation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string AssignedUserId { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsAssigned { get; set; } = false;
        public string Source { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true; // New field

        // Navigation property for related messages
        public ICollection<Message> Messages { get; set; } = new List<Message>();

    }
}