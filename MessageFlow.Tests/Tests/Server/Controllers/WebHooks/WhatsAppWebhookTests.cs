using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MediatR;
using System.Text.Json;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;
using MessageFlow.Server.Configuration;

namespace MessageFlow.Tests.Tests.Server.Controllers;

public class WhatsAppWebhookTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<WhatsAppWebhook>> _loggerMock = new();
    private readonly WhatsAppWebhook _controller;

    public WhatsAppWebhookTests()
    {
        var options = Options.Create(new GlobalChannelSettings
        {
            WhatsAppWebhookVerifyToken = "verify_token"
        });

        _controller = new WhatsAppWebhook(_mediatorMock.Object, _loggerMock.Object, options);
    }

    [Fact]
    public void Verify_ReturnsOk_WhenTokenMatches()
    {
        var result = _controller.Verify("subscribe", "challenge", "verify_token");
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("challenge", ok.Value);
    }

    [Fact]
    public void Verify_ReturnsUnauthorized_WhenModeInvalid()
    {
        var result = _controller.Verify("wrong", "challenge", "verify_token");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void Verify_ReturnsUnauthorized_WhenTokenInvalid()
    {
        var result = _controller.Verify("subscribe", "challenge", "wrong_token");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Receive_ReturnsOk_WhenProcessingSucceeds()
    {
        var json = JsonDocument.Parse("""
        {
            "object": "whatsapp_business_account",
            "entry": [
                {
                    "id": "abc",
                    "changes": []
                }
            ]
        }
        """).RootElement;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessIncomingWAMessageCommand>(), default))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.Receive(json);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Receive_ReturnsBadRequest_OnException()
    {
        // This JSON is missing required structure to force exception at entry level
        var invalidJson = JsonDocument.Parse("""{ "unexpected": "value" }""").RootElement;

        var result = await _controller.Receive(invalidJson);

        Assert.IsType<BadRequestResult>(result);
    }
}