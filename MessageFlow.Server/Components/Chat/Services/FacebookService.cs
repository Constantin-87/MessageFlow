using MessageFlow.Server.Components.Chat.Helpers;
using MessageFlow.DataAccess.Services;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using AutoMapper;
using MessageFlow.Shared.Interfaces;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Infrastructure.Mediator;
using MessageFlow.Infrastructure.Mediator.Commands;
using MessageFlow.Infrastructure.Mediator.Commands.Chat;

namespace MessageFlow.Server.Components.Chat.Services
{
    public class FacebookService : IFacebookService
    {
        private readonly ILogger<FacebookService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public FacebookService(
            ILogger<FacebookService> logger,
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> chatHub,
            IMediator mediator,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _chatHub = chatHub;
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
        }

        // Retrieve Facebook settings for a company
        public async Task<FacebookSettingsDTO?> GetFacebookSettingsAsync(string companyId)
        {
            var settings = await _unitOfWork.FacebookSettings.GetSettingsByCompanyIdAsync(companyId);
            return _mapper.Map<FacebookSettingsDTO>(settings);
        }

        // Save Facebook settings for a company
        public async Task<bool> SaveFacebookSettingsAsync(string companyId, FacebookSettingsDTO facebookSettingsDto)
        {
            var existingSettings = await _unitOfWork.FacebookSettings.GetSettingsByCompanyIdAsync(companyId);

            if (existingSettings == null)
            {
                // Create new settings
                var newSettings = _mapper.Map<FacebookSettingsModel>(facebookSettingsDto);
                newSettings.CompanyId = companyId;
                await _unitOfWork.FacebookSettings.AddEntityAsync(newSettings);
            }
            else
            {
                // Update existing settings
                existingSettings.PageId = facebookSettingsDto.PageId;
                existingSettings.AccessToken = facebookSettingsDto.AccessToken;
                await _unitOfWork.FacebookSettings.UpdateEntityAsync(existingSettings);            
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<FacebookSettingsDTO?> GetFacebookSettingsByPageIdAsync(string pageId)
        {
            var settings = await _unitOfWork.FacebookSettings.GetSettingsByPageIdAsync(pageId);
            return _mapper.Map<FacebookSettingsDTO>(settings);
        }


        public async Task SendMessageToFacebookAsync(string recipientId, string messageText, string companyId, string localMessageId)
        {
            var facebookSettings = await _unitOfWork.FacebookSettings.GetSettingsByCompanyIdAsync(companyId);

            if (facebookSettings != null)
            {

                var url = $"https://graph.facebook.com/v11.0/me/messages";
                var payload = new
                {
                    recipient = new { id = recipientId },
                    messaging_type = "RESPONSE",
                    message = new { text = messageText }
                };

                var response = await MessageSenderHelper.SendMessageAsync(url, payload, facebookSettings.AccessToken, _logger);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseJson = JsonDocument.Parse(responseBody);

                    var facebookMessageId = responseJson.RootElement.GetProperty("message_id").GetString();

                    // Update local database with FacebookMessageId
                    var message = await _unitOfWork.Messages.GetMessageByIdAsync(localMessageId);
                    if (message != null)
                    {
                        message.ProviderMessageId = facebookMessageId;
                        _unitOfWork.Messages.UpdateEntityAsync(message);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    _logger.LogInformation($"Message sent to Facebook. Message ID: {facebookMessageId}");
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send Facebook message: {responseBody}");

                    var errorDetails = JsonDocument.Parse(responseBody).RootElement;
                    var errorMessage = errorDetails.GetProperty("error").GetProperty("message").GetString();
                    var statusElement = JsonDocument.Parse($"{{\"id\":\"{localMessageId}\",\"status\":\"error\",\"errors\":[{{\"message\":\"{errorMessage}\"}}]}}").RootElement;
                    //await _messageProcessingService.ProcessMessageStatusUpdateAsync(statusElement, "Facebook");
                    await _mediator.Send(new ProcessMessageStatusUpdateCommand(statusElement, "Facebook"));
                }
            }
            else
            {
                Console.WriteLine($"Facebook settings not found for company ID {companyId}.");
            }
        }

        public async Task ProcessFacebookWebhookEventAsync(JsonElement entry)
        {
            if (!entry.TryGetProperty("messaging", out var messagingEvents))
                return;

            foreach (var messagingEvent in messagingEvents.EnumerateArray())
            {
                if (messagingEvent.TryGetProperty("delivery", out var delivery))
                {
                    await HandleDeliveryEvent(delivery);
                }
                else if (messagingEvent.TryGetProperty("read", out var read))
                {
                    var senderId = messagingEvent.GetProperty("sender").GetProperty("id").GetString();
                    var recipientId = messagingEvent.GetProperty("recipient").GetProperty("id").GetString();
                    await HandleReadEvent(read, senderId, recipientId);
                }
                else if (messagingEvent.TryGetProperty("message", out var messageElement))
                {
                    if (messageElement.TryGetProperty("is_echo", out var isEcho) && isEcho.GetBoolean())
                    {
                        // Log and skip processing for echo messages
                        _logger.LogInformation($"Ignoring echo message with ID: {messageElement.GetProperty("mid").GetString()}");
                        continue;
                    }

                    // Process regular messages
                    var pageId = entry.GetProperty("id").GetString();
                    await ProcessFacebookMessagesAsync(pageId, new[] { messagingEvent });
                }
            }
        }

        private async Task HandleDeliveryEvent(JsonElement delivery)
        {
            if (!delivery.TryGetProperty("mids", out var mids))
                return;

            foreach (var mid in mids.EnumerateArray())
            {
                var statusElement = JsonDocument.Parse($"{{\"id\":\"{mid.GetString()}\",\"status\":\"delivered\"}}").RootElement;
                //await _messageProcessingService.ProcessMessageStatusUpdateAsync(statusElement, "Facebook");
                await _mediator.Send(new ProcessMessageStatusUpdateCommand(statusElement, "Facebook"));

            }
        }

        private async Task HandleReadEvent(JsonElement read, string senderId, string recipientId)
        {
            if (!read.TryGetProperty("watermark", out var watermarkProperty))
            {
                _logger.LogWarning("No 'watermark' property found in the 'read' event.");
                return;
            }

            long watermarkUnix;
            try
            {
                // Extract the watermark and validate it's within a reasonable range
                watermarkUnix = watermarkProperty.GetInt64();
                if (watermarkUnix > DateTimeOffset.MaxValue.ToUnixTimeMilliseconds() || watermarkUnix < DateTimeOffset.MinValue.ToUnixTimeMilliseconds())
                {
                    _logger.LogWarning($"Invalid watermark timestamp: {watermarkUnix}. Skipping processing.");
                    return;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Failed to parse watermark: {ex.Message}");
                return;
            }

            var watermarkTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(watermarkUnix).UtcDateTime;

            // ✅ Retrieve Facebook settings using Unit of Work
            var facebookSettings = await _unitOfWork.FacebookSettings.GetSettingsByPageIdAsync(recipientId);


            if (facebookSettings == null)
            {
                _logger.LogWarning($"No Facebook settings found for Page ID {recipientId}.");
                return;
            }

            // ✅ Use the CompanyId and SenderId to find the conversation
            var conversation = await _unitOfWork.Conversations.GetConversationBySenderAndCompanyAsync(senderId, facebookSettings.CompanyId);

            if (conversation == null)
            {
                _logger.LogWarning($"No conversation found for sender {senderId} in company {facebookSettings.CompanyId}.");
                return;
            }

            // ✅ Find messages in this conversation sent before the watermark and not yet marked as read
            var messagesToUpdate = await _unitOfWork.Messages.GetUnreadMessagesBeforeTimestampAsync(conversation.Id, watermarkTimestamp);


            foreach (var message in messagesToUpdate)
            {
                try
                {
                    // Format the JSON with the timestamp
                    var statusElementJson = $"{{\"id\":\"{message.ProviderMessageId}\",\"status\":\"read\",\"timestamp\":\"{watermarkUnix / 1000}\"}}";
                    var statusElement = JsonDocument.Parse(statusElementJson).RootElement;
                    await _mediator.Send(new ProcessMessageStatusUpdateCommand(statusElement, "Facebook"));

                    //await _messageProcessingService.ProcessMessageStatusUpdateAsync(statusElement, "Facebook");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating message ID {message.Id} to 'read': {ex.Message}");
                }
            }
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Marked {messagesToUpdate.Count} messages as read in conversation {conversation.Id} up to watermark timestamp {watermarkTimestamp}.");
        }

        public async Task ProcessFacebookMessagesAsync(string pageId, IEnumerable<JsonElement> messagingArray)
        {
            var facebookSettings = await GetFacebookSettingsByPageIdAsync(pageId);
            var companyId = facebookSettings?.CompanyId ?? string.Empty;

            if (string.IsNullOrEmpty(companyId))
            {
                _logger.LogWarning($"No Facebook settings found for Page ID {pageId}");
                return;
            }

            foreach (var eventData in messagingArray)
            {
                if (eventData.TryGetProperty("sender", out var senderElement) &&
                    eventData.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("mid", out var midElement))
                {

                    var senderId = senderElement.GetProperty("id").GetString();
                    var messageText = messageElement.GetProperty("text").GetString();
                    var providerMessageId = midElement.GetString();

                    var senderUserName = senderId; // Placeholder; To be adjusted to fetch the actual username

                    await _mediator.Send(new ProcessMessageCommand(companyId, senderId, senderUserName, messageText, providerMessageId, "Facebook"));

                    //await _messageProcessingService.ProcessMessageAsync(companyId, senderId, senderUserName, messageText, providerMessageId, "Facebook");
                }
                else
                {
                    _logger.LogWarning($"Unhandled event type in Facebook webhook payload: {eventData}");
                }
            }

        }
    }
}
