using Moq;
using AutoMapper;
using MediatR;
using MessageFlow.Shared.DTOs;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;
using Microsoft.Extensions.Logging;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class SendMessageToCustomerHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly SendMessageToCustomerHandler _handler;
    private readonly Mock<ILogger<SendMessageToCustomerHandler>> _loggerMock = new();

    public SendMessageToCustomerHandlerTests()
    {
        _handler = new SendMessageToCustomerHandler(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_MessageDtoIsNull_ReturnsFalse()
    {
        var result = await _handler.Handle(new SendMessageToCustomerCommand(null!), default);

        Assert.False(result.Success);
        Assert.Equal("Message is null.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsFalse()
    {
        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationByIdAsync("conv1"))
            .ReturnsAsync((Conversation?)null);

        var dto = new MessageDTO { ConversationId = "conv1" };
        var result = await _handler.Handle(new SendMessageToCustomerCommand(dto), default);

        Assert.False(result.Success);
        Assert.Equal("Conversation conv1 not found.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("Facebook")]
    [InlineData("WhatsApp")]
    public async Task Handle_SuccessfulSendToPlatform_ReturnsTrue(string platform)
    {
        var dto = new MessageDTO { Id = "m1", ConversationId = "conv1", Content = "Hello" };
        var conversation = new Conversation
        {
            Id = "conv1",
            CompanyId = "comp1",
            SenderId = "user1",
            Source = platform
        };
        var message = new Message { Id = "m1", Content = "Hello", ConversationId = "conv1" };

        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationByIdAsync("conv1")).ReturnsAsync(conversation);
        _mapperMock.Setup(m => m.Map<Message>(dto)).Returns(message);
        _unitOfWorkMock.Setup(u => u.Messages.AddEntityAsync(message)).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new SendMessageToCustomerCommand(dto), default);

        Assert.True(result.Success);
        Assert.Equal("Message sent.", result.ErrorMessage);

        if (platform == "Facebook")
        {
            _mediatorMock.Verify(m => m.Send(It.Is<SendMessageToFacebookCommand>(x =>
                x.CompanyId == "comp1" &&
                x.RecipientId == "user1" &&
                x.MessageText == "Hello" &&
                x.LocalMessageId == "m1"), default), Times.Once);
        }
        else
        {
            _mediatorMock.Verify(m => m.Send(It.Is<SendMessageToWhatsAppCommand>(x =>
                x.CompanyId == "comp1" &&
                x.RecipientPhoneNumber == "user1" &&
                x.MessageText == "Hello" &&
                x.LocalMessageId == "m1"), default), Times.Once);
        }
    }

    [Fact]
    public async Task Handle_UnknownSource_ReturnsFalse()
    {
        var conversation = new Conversation
        {
            Id = "conv1",
            Source = "Telegram",
            CompanyId = "comp1",
            SenderId = "user1"
        };

        var dto = new MessageDTO
        {
            Id = "m1",
            ConversationId = "conv1",
            Content = "Hi",
            SentAt = DateTime.UtcNow,
            UserId = "user1",
            Username = "Test User",
            Status = "pending",
            ChangedAt = DateTime.UtcNow,
            Conversation = new ConversationDTO { Id = "conv1", Source = "Telegram" }
        };

        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationByIdAsync("conv1"))
            .ReturnsAsync(conversation);

        _unitOfWorkMock.Setup(u => u.Messages.AddEntityAsync(It.IsAny<Message>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new SendMessageToCustomerCommand(dto), default);

        Assert.False(result.Success);
        Assert.Equal("Unknown source: Telegram", result.ErrorMessage);
    }
}