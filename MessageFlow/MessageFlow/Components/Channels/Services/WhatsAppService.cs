using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace MessageFlow.Components.Channels.Services
{
    public class WhatsAppService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _chatHub;

        public WhatsAppService(ApplicationDbContext dbContext, IHubContext<ChatHub> chatHub)
        {
            _dbContext = dbContext;
            _chatHub = chatHub;
        }
        public async Task<bool> SaveWhatsAppSettingsAsync(int companyId, WhatsAppSettingsModel whatsAppSettings)
        {
            var existingSettings = await GetWhatsAppSettingsAsync(companyId);

            if (existingSettings == null)
            {
                // Create new settings
                whatsAppSettings.CompanyId = companyId;
                _dbContext.WhatsAppSettingsModels.Add(whatsAppSettings);
            }
            else
            {
                // Update existing settings
                existingSettings.AccessToken = whatsAppSettings.AccessToken;
                existingSettings.BusinessAccountId = whatsAppSettings.BusinessAccountId;
                existingSettings.PhoneNumbers = whatsAppSettings.PhoneNumbers;
                existingSettings.WebhookVerifyToken = whatsAppSettings.WebhookVerifyToken;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<WhatsAppSettingsModel>> GetAllWhatsAppSettingsAsync()
        {
            return await _dbContext.WhatsAppSettingsModels.ToListAsync();
        }

        public async Task<WhatsAppSettingsModel?> GetWhatsAppSettingsByBusinessAccountIdAsync(string businessAccountId)
        {
            return await _dbContext.WhatsAppSettingsModels
                .FirstOrDefaultAsync(ws => ws.BusinessAccountId == businessAccountId);
        }


        // Retrieve WhatsApp settings for a company
        public async Task<WhatsAppSettingsModel?> GetWhatsAppSettingsAsync(int companyId)
        {
            return await _dbContext.WhatsAppSettingsModels
                .Include(ws => ws.PhoneNumbers)
                .FirstOrDefaultAsync(ws => ws.CompanyId == companyId);
        }


        public async Task ProcessIncomingMessageAsync(string senderPhoneNumber, string messageText, int companyId)
        {
            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.SenderId == senderPhoneNumber && c.CompanyId == companyId.ToString());

            if (conversation != null && conversation.IsActive)
            {
                await AddMessageToConversation(conversation, senderPhoneNumber, messageText);
            }
            else
            {
                await CreateAndBroadcastNewConversation(companyId, senderPhoneNumber, messageText);
            }
        }

        private async Task AddMessageToConversation(Conversation conversation, string senderPhoneNumber, string messageText)
        {
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversation.Id,
                UserId = conversation.AssignedUserId,
                Content = messageText,
                SentAt = DateTime.UtcNow
            };

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            if (!string.IsNullOrEmpty(conversation.AssignedUserId))
            {
                await _chatHub.Clients.User(conversation.AssignedUserId)
                    .SendAsync("ReceiveMessage", messageText, senderPhoneNumber);
            }
        }

        private async Task CreateAndBroadcastNewConversation(int companyId, string senderPhoneNumber, string messageText)
        {
            var conversation = new Conversation
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = senderPhoneNumber,
                CompanyId = companyId.ToString(),
                Title = $"Chat with {senderPhoneNumber}",
                IsActive = true,
                Source = "WhatsApp"
            };

            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync();

            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversation.Id,
                UserId = senderPhoneNumber,
                Content = messageText,
                SentAt = DateTime.UtcNow
            };

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            await _chatHub.Clients.Group($"Company_{companyId}")
                .SendAsync("NewConversationAdded", conversation);
        }

        // Send a message via WhatsApp Cloud API
        public async Task SendMessageToWhatsAppAsync(string recipientPhoneNumber, string messageText, string companyId, string localMessageId)
        {
            var whatsAppSettings = await GetWhatsAppSettingsAsync(int.Parse(companyId));

            if (whatsAppSettings != null && whatsAppSettings.PhoneNumbers.Any())
            {
                var phoneNumberInfo = whatsAppSettings.PhoneNumbers.FirstOrDefault();
                if (phoneNumberInfo != null)
                {
                    var httpClient = new HttpClient();
                    var jsonMessage = new
                    {
                        messaging_product = "whatsapp",
                        to = recipientPhoneNumber,
                        type = "text",
                        text = new { body = messageText }
                    };

                    var jsonContent = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");

                    Console.WriteLine($"Sending HTTP POST to WhatsApp: {jsonContent}");

                    var response = await httpClient.PostAsync(
                        $"https://graph.facebook.com/v17.0/{phoneNumberInfo.PhoneNumberId}/messages?access_token={whatsAppSettings.AccessToken}",
                        jsonContent
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Message sent successfully to {recipientPhoneNumber}.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to send message to {recipientPhoneNumber}: {await response.Content.ReadAsStringAsync()}");
                    }
                }
                else
                {
                    Console.WriteLine("No phone number found in the settings.");
                }
            }
            else
            {
                Console.WriteLine($"WhatsApp settings not found for company ID {companyId}.");
            }
        }

    }
}
