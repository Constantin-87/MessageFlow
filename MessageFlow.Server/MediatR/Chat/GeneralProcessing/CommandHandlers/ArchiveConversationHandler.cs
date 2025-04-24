using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;

public class ArchiveConversationHandler : IRequestHandler<ArchiveConversationCommand, (bool Success, string ErrorMessage)>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _salt;

    public ArchiveConversationHandler(IUnitOfWork unitOfWork, IConfiguration config)
    {
        _unitOfWork = unitOfWork;
        _salt = config["chat-archive-salt"] ?? throw new InvalidOperationException("Missing salt config value");
    }

    public async Task<(bool Success, string ErrorMessage)> Handle(ArchiveConversationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await _unitOfWork.Conversations.GetConversationBySenderIdAsync(request.CustomerId);
            if (conversation == null)
                return (false, $"No conversation found for sender ID: {request.CustomerId}");

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

            await _unitOfWork.ArchivedConversations.AddEntityAsync(archivedConversation);
            await _unitOfWork.Conversations.RemoveEntityAsync(conversation);
            await _unitOfWork.SaveChangesAsync();

            return (true, $"Chat with customer {request.CustomerId} archived and closed.");
        }
        catch (Exception ex)
        {
            return (false, $"Error archiving chat: {ex.Message}");
        }
    }

    private string GeneratePseudonymizedId(string senderId) => Convert.ToBase64String(
        SHA256.HashData(Encoding.UTF8.GetBytes(senderId + _salt))
    ).Substring(0, 22);

    private string AnonymizeContent(string content)
    {
        content = Regex.Replace(content, @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", "[REDACTED EMAIL]", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"\b\d{3}[-.\s]?\d{3}[-.\s]?\d{4}\b", "[REDACTED PHONE]");
        return content;
    }
}