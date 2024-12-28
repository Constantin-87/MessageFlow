﻿namespace MessageFlow.Client.Models
{
    public class Message
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string ConversationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        // Property to hold error message (set when there’s an error)
        public string? ErrorMessage { get; set; }

        // Navigation property
        public Conversation Conversation { get; set; }

        public string StatusIcon
        {
            get
            {
                return Status switch
                {
                    "SentToProvider" => "images/sent.png",
                    "sent" => "images/sent.png",
                    "delivered" => "images/delivered.png",
                    "read" => "images/read.png",
                    "error" => "images/error.png",
                    _ => "images/unknown.png", // Default icon if status is unrecognized
                };
            }
        }

        public string Tooltip
        {
            get
            {
                return Status switch
                {
                    "SentToProvider" => "Message has been sent to the provider.",
                    "sent" => "Message has been sent to user.",
                    "delivered" => "Message has been delivered.",
                    "read" => "Message has been read.",
                    "error" => ErrorMessage ?? "An error occurred.", // Show the error message if available
                    _ => "Unknown status.", // Default tooltip
                };
            }
        }

    }
}
