using MediatR;
using Microsoft.Extensions.Configuration;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using System.Text;
using Xunit;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class ArchiveConversationHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly IConfiguration _config;
    private readonly ArchiveConversationHandler _handler;

    public ArchiveConversationHandlerTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "chat-archive-salt", "s@lt" }
            }).Build();

        _handler = new ArchiveConversationHandler(_unitOfWorkMock.Object, _config);
    }

    [Fact]
    public async Task Handle_ValidConversation_ArchivesSuccessfully()
    {
        var messages = new List<Message>
        {
            new() { Id = "m1", ConversationId = "c1", UserId = "cust123", Content = "hello user@email.com", SentAt = DateTime.UtcNow }
        };

        var conversation = new Conversation
        {
            Id = "c1",
            SenderId = "cust123",
            CreatedAt = DateTime.UtcNow,
            CompanyId = "comp1",
            AssignedUserId = "agent1",
            Source = "Web",
            Messages = messages
        };

        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationBySenderIdAsync("cust123")).ReturnsAsync(conversation);
        _unitOfWorkMock.Setup(u => u.ArchivedConversations.AddEntityAsync(It.IsAny<ArchivedConversation>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.Conversations.RemoveEntityAsync(conversation)).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new ArchiveConversationCommand("cust123"), default);

        Assert.True(result.Success);
        Assert.Contains("archived", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsFalse()
    {
        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationBySenderIdAsync("cust999")).ReturnsAsync((Conversation?)null);

        var result = await _handler.Handle(new ArchiveConversationCommand("cust999"), default);

        Assert.False(result.Success);
        Assert.Contains("No conversation found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ExceptionDuringProcessing_ReturnsError()
    {
        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationBySenderIdAsync("cust123"))
            .ThrowsAsync(new Exception("DB down"));

        var result = await _handler.Handle(new ArchiveConversationCommand("cust123"), default);

        Assert.False(result.Success);
        Assert.Contains("Error archiving chat", result.ErrorMessage);
    }
}
