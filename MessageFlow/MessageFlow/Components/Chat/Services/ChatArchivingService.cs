using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.Design;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MessageFlow.Components.Chat.Services
{
    public class ChatArchivingService
    {
        private readonly ApplicationDbContext _dbContext;
        private const string Salt = "YourSecretSaltHere"; // Replace with your own fixed, secret salt

        public ChatArchivingService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Method to generate a consistent, pseudonymized ID
        private string GeneratePseudonymizedId(string senderId)
        {
            using (var sha256 = SHA256.Create())
            {
                var combinedInput = senderId + Salt;
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedInput));

                // Convert to Base64 and take the first half of the hash
                var hashString = Convert.ToBase64String(hashBytes);
                return hashString.Substring(0, hashString.Length / 2);
            }
        }

        // Method to anonymize the content by removing sensitive data
        private string AnonymizeContent(string content)
        {
            // Example: Remove email addresses and phone numbers
            content = Regex.Replace(content, @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", "[REDACTED EMAIL]", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"\b\d{3}[-.\s]?\d{3}[-.\s]?\d{4}\b", "[REDACTED PHONE]");

            // Replace any other identifiable information as needed
            return content;
        }

        public async Task ArchiveConversationAsync(string customerId)
        {
            var conversation = await _dbContext.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.SenderId == customerId);

            if (conversation != null)
            {
                // Pseudonymize the SenderId
                string pseudonymizedSenderId = GeneratePseudonymizedId(conversation.SenderId);

                var archivedConversation = new ArchivedConversation
                {
                    Id = conversation.Id,
                    Source = conversation.Source,
                    CreatedAt = conversation.CreatedAt,
                    AssignedUserId = conversation.AssignedUserId,
                    CompanyId = conversation.CompanyId,
                    Messages = conversation.Messages.Select(m => new ArchivedMessage
                    {
                        Id = m.Id,
                        ArchivedConversationId = m.ConversationId,
                        UserId = m.UserId == conversation.SenderId ? pseudonymizedSenderId : m.UserId,
                        Content = AnonymizeContent(m.Content),
                        SentAt = m.SentAt
                    }).ToList()
                };

                // Add the archived conversation and remove the original
                _dbContext.ArchivedConversations.Add(archivedConversation);
                _dbContext.Conversations.Remove(conversation);

                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
