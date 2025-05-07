using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MessageFlow.Server.Configuration;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;

namespace MessageFlow.Tests.UnitTests.Server.Controllers;

public class FacebookWebhookTests
{
    private readonly Mock<ILogger<FacebookWebhook>> _loggerMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly FacebookWebhook _controller;

    public FacebookWebhookTests()
    {
        var settings = Options.Create(new GlobalChannelSettings
        {
            FacebookWebhookVerifyToken = "valid-token"
        });

        _controller = new FacebookWebhook(_loggerMock.Object, _mediatorMock.Object, settings);
    }

    [Fact]
    public void Verify_ReturnsOk_WhenTokenIsValid()
    {
        var result = _controller.Verify("subscribe", "challenge123", "valid-token");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("challenge123", okResult.Value);
    }

    [Fact]
    public void Verify_ReturnsUnauthorized_WhenModeIsInvalid()
    {
        var result = _controller.Verify("invalid", "challenge123", "valid-token");

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void Verify_ReturnsUnauthorized_WhenTokenIsInvalid()
    {
        var result = _controller.Verify("subscribe", "challenge123", "wrong-token");

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Receive_ReturnsOk_OnSuccess()
    {
        // Arrange
        var json = """
        {
            "object": "page",
            "entry": [
                {
                    "id": "page_id",
                    "time": 1234567890,
                    "messaging": [
                        {
                            "sender": { "id": "user_id" },
                            "recipient": { "id": "page_id" },
                            "timestamp": 1234567890,
                            "message": { "mid": "mid.123", "text": "Hello!" }
                        }
                    ]
                }
            ]
        }
        """;

        var body = JsonDocument.Parse(json).RootElement;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessFacebookWebhookEventCommand>(), default))
            .ReturnsAsync(Unit.Value);

        // Act
        var result = await _controller.Receive(body);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Receive_ReturnsBadRequest_OnException()
    {
        // Pass an invalid/malformed JsonElement to trigger the outer catch block
        var malformed = JsonDocument.Parse("{}").RootElement;

        var result = await _controller.Receive(malformed);

        Assert.IsType<BadRequestResult>(result);
    }
}