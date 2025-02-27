using MessageFlow.Server.Components.Chat.Helpers;
using MessageFlow.DataAccess.Services;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using AutoMapper;
using MessageFlow.Shared.Interfaces;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Infrastructure.Mediator.Commands;
using MessageFlow.Infrastructure.Mediator.Commands.Chat;

namespace MessageFlow.Server.Components.Chat.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public WhatsAppService(
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> chatHub,
            ILogger<WhatsAppService> logger,
            IMediator mediator,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _chatHub = chatHub;
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
        }

        public async Task<bool> SaveWhatsAppSettingsAsync(string companyId, WhatsAppSettingsDTO whatsAppSettingsDto)
        {
            var existingSettings = await _unitOfWork.WhatsAppSettings.GetSettingsByCompanyIdAsync(companyId);

            if (existingSettings == null)
            {
                // Create new settings
                var newSettings = _mapper.Map<WhatsAppSettingsModel>(whatsAppSettingsDto);
                newSettings.CompanyId = companyId;
                await _unitOfWork.WhatsAppSettings.AddEntityAsync(newSettings);
            }
            else
            {
                // Update existing settings
                existingSettings.AccessToken = whatsAppSettingsDto.AccessToken;
                existingSettings.BusinessAccountId = whatsAppSettingsDto.BusinessAccountId;
                existingSettings.PhoneNumbers = _mapper.Map<List<PhoneNumberInfo>>(whatsAppSettingsDto.PhoneNumbers);

                _unitOfWork.WhatsAppSettings.UpdateEntityAsync(existingSettings);

            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<WhatsAppSettingsDTO?> GetWhatsAppSettingsByBusinessAccountIdAsync(string businessAccountId)
        {
            var settings = await _unitOfWork.WhatsAppSettings.GetSettingsByBusinessAccountIdAsync(businessAccountId);
            return _mapper.Map<WhatsAppSettingsDTO>(settings);
        }

        // Retrieve WhatsApp settings for a company
        public async Task<WhatsAppSettingsDTO?> GetWhatsAppSettingsAsync(string companyId)
        {
            var settings = await _unitOfWork.WhatsAppSettings.GetSettingsByCompanyIdAsync(companyId);
            return _mapper.Map<WhatsAppSettingsDTO>(settings);
        }

        public async Task ProcessIncomingMessageAsync(string businessAccountId, JsonElement changes)
        {
            var whatsAppSettings = await GetWhatsAppSettingsByBusinessAccountIdAsync(businessAccountId);
            
            var companyId = whatsAppSettings?.CompanyId ?? null;

            if (string.IsNullOrEmpty(companyId))
            {
                _logger.LogWarning($"No WhatsApp settings found for BusinessAccountId {businessAccountId}");
                return;
            }

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value))
                {
                    _logger.LogWarning($"No value found in change entry for BusinessAccountId {businessAccountId}");
                    continue;
                }

                // Handle delivery statuses
                if (value.TryGetProperty("statuses", out var statuses))
                {
                    var sortedStatuses = statuses.EnumerateArray()
                                                 .OrderBy(s => s.GetProperty("timestamp").GetString())
                                                 .ToList();

                    foreach (var status in sortedStatuses)
                    {
                        await _mediator.Send(new ProcessMessageStatusUpdateCommand(status, "WhatsApp"));

                        //await _messageProcessingService.ProcessMessageStatusUpdateAsync(status, "WhatsApp");
                    }
                }

                if (value.TryGetProperty("contacts", out var contacts))
                {
                    foreach (var contact in contacts.EnumerateArray())
                    {
                        var senderId = contact.GetProperty("wa_id").GetString();
                        var username = contact.GetProperty("profile").GetProperty("name").GetString();

                        if (value.TryGetProperty("messages", out var messages))
                        {
                            foreach (var message in messages.EnumerateArray())
                            {
                                var messageText = message.GetProperty("text").GetProperty("body").GetString();
                                var providerMessageId = message.GetProperty("id").GetString();

                                await _mediator.Send(new ProcessMessageCommand(companyId, senderId, username, messageText, providerMessageId, "WhatsApp"));

                                //await _messageProcessingService.ProcessMessageAsync(companyId, senderId, username, messageText, providerMessageId, "WhatsApp");
                            }
                        }
                    }
                }
            }
        }

        // Send a message via WhatsApp Cloud API
        public async Task SendMessageToWhatsAppAsync(string recipientPhoneNumber, string messageText, string companyId, string localMessageId)
        {
            var whatsAppSettings = await _unitOfWork.WhatsAppSettings.GetSettingsByCompanyIdAsync(companyId);

            if (whatsAppSettings != null && whatsAppSettings.PhoneNumbers.Any())
            {
                var phoneNumberInfo = whatsAppSettings.PhoneNumbers.FirstOrDefault();
                if (phoneNumberInfo != null)
                {
                    var url = $"https://graph.facebook.com/v17.0/{phoneNumberInfo.PhoneNumberId}/messages";
                    var payload = new
                    {
                        messaging_product = "whatsapp",
                        to = recipientPhoneNumber,
                        type = "text",
                        text = new { body = messageText }
                    };

                    var response = await MessageSenderHelper.SendMessageAsync(url, payload, whatsAppSettings.AccessToken, _logger);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseJson = JsonDocument.Parse(responseBody);

                        var whatsappMessageId = responseJson.RootElement
                            .GetProperty("messages")[0]
                            .GetProperty("id")
                            .GetString();

                        // Update local database with WhatsAppMessageId
                        //var message = await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == localMessageId);
                        var message = await _unitOfWork.Messages.GetMessageByIdAsync(localMessageId);
                        if (message != null)
                        {
                            //message.ProviderMessageId = whatsappMessageId;
                            //await _dbContext.SaveChangesAsync();
                            message.ProviderMessageId = whatsappMessageId;
                            _unitOfWork.Messages.UpdateEntityAsync(message);
                            await _unitOfWork.SaveChangesAsync();
                        }

                        // Notify app user of the message status
                        await _chatHub.Clients.User(companyId)
                            .SendAsync("MessageStatusUpdated", localMessageId, "Sent");
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Failed to send WhatsApp message: {responseBody}");

                        var errorDetails = JsonDocument.Parse(responseBody).RootElement;
                        var errorMessage = errorDetails.GetProperty("error").GetProperty("message").GetString();
                        var jsonElement = JsonDocument.Parse($"{{\"id\":\"{localMessageId}\",\"status\":\"error\",\"errors\":[{{\"message\":\"{errorMessage}\"}}]}}").RootElement;

                        //await _messageProcessingService.ProcessMessageStatusUpdateAsync(jsonElement, "WhatsApp");
                        await _mediator.Send(new ProcessMessageStatusUpdateCommand(jsonElement, "WhatsApp"));


                        _logger.LogError($"Failed to send WhatsApp message to {recipientPhoneNumber}");
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

        // TO DO: Mark messages as read when clicking into the chat window 

        //public async Task MarkMessagesAsReadAsync(string whatsappConversationId, List<string> messageIds)
        //{

        //    // Retrieve WhatsApp settings for the given conversation ID
        //    var whatsAppSettings = await _dbContext.WhatsAppSettingsModels
        //        .FirstOrDefaultAsync(ws => ws.BusinessAccountId == whatsappConversationId);

        //    if (whatsAppSettings == null)
        //    {
        //        _logger.LogWarning($"No WhatsApp settings found for BusinessAccountId {whatsappConversationId}");
        //        return;
        //    }

        //    var url = $"https://graph.facebook.com/v17.0/{whatsappConversationId}/messages";
        //    var payload = new
        //    {
        //        messaging_product = "whatsapp",
        //        status = "read",
        //        ids = messageIds
        //    };

        //    await MessageSenderHelper.SendMessageAsync(url, payload, whatsAppSettings.AccessToken, _logger);
        //}
    }
}
