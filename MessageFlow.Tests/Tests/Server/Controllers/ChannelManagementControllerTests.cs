using Xunit;
using Moq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MessageFlow.Server.Controllers;
using MessageFlow.Server.DataTransferObjects.Client;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Queries;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Queries;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Tests.Tests.Server.Controllers;

public class ChannelManagementControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ChannelManagementController _controller;

    public ChannelManagementControllerTests()
    {
        _controller = new ChannelManagementController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetFacebookSettings_ReturnsOk_WhenFound()
    {
        var companyId = "c1";
        var expected = new Shared.DTOs.FacebookSettingsDTO { PageId = "pg1", AccessToken = "abc", CompanyId = companyId };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetFacebookSettingsQuery>(q => q.CompanyId == companyId), default))
            .ReturnsAsync(expected);

        var result = await _controller.GetFacebookSettings(companyId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetFacebookSettings_ReturnsNotFound_WhenNull()
    {
        var companyId = "c1";

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetFacebookSettingsQuery>(q => q.CompanyId == companyId), default))
            .ReturnsAsync((Shared.DTOs.FacebookSettingsDTO?)null);

        var result = await _controller.GetFacebookSettings(companyId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SaveFacebookSettings_ReturnsOk_WhenSuccess()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<SaveFacebookSettingsCommand>(), default))
            .ReturnsAsync((true, "Saved"));

        var result = await _controller.SaveFacebookSettings("c1", new Shared.DTOs.FacebookSettingsDTO());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Saved", ok.Value);
    }

    [Fact]
    public async Task SaveFacebookSettings_ReturnsBadRequest_WhenFailed()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<SaveFacebookSettingsCommand>(), default))
            .ReturnsAsync((false, "Error"));

        var result = await _controller.SaveFacebookSettings("c1", new Shared.DTOs.FacebookSettingsDTO());

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Error", bad.Value);
    }

    [Fact]
    public async Task GetWhatsAppSettings_ReturnsOk_WhenFound()
    {
        var expected = new WhatsAppSettingsDTO();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetWhatsAppSettingsQuery>(), default)).ReturnsAsync(expected);

        var result = await _controller.GetWhatsAppSettings("c1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetWhatsAppSettings_ReturnsNotFound_WhenNull()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetWhatsAppSettingsQuery>(), default)).ReturnsAsync((WhatsAppSettingsDTO?)null);

        var result = await _controller.GetWhatsAppSettings("c1");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SaveCoreSettings_ReturnsOk_WhenSuccess()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<SaveWhatsAppCoreSettingsCommand>(), default))
            .ReturnsAsync((true, "Saved"));

        var dto = new WhatsAppCoreSettingsDTO { CompanyId = "c1", AccessToken = "token", BusinessAccountId = "bid" };

        var result = await _controller.SaveCoreSettings(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Saved", ok.Value);
    }

    [Fact]
    public async Task SaveCoreSettings_ReturnsBadRequest_WhenFailed()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<SaveWhatsAppCoreSettingsCommand>(), default))
            .ReturnsAsync((false, "Failed"));

        var result = await _controller.SaveCoreSettings(new WhatsAppCoreSettingsDTO());

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed", bad.Value);
    }

    [Fact]
    public async Task SavePhoneNumbers_ReturnsOk_WhenSuccess()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<SaveWhatsAppPhoneNumbersCommand>(), default))
            .ReturnsAsync((true, "Saved"));

        var result = await _controller.SavePhoneNumbers(new List<MessageFlow.Server.DataTransferObjects.Client.PhoneNumberInfoDTO>());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Saved", ok.Value);
    }

    [Fact]
    public async Task SavePhoneNumbers_ReturnsBadRequest_WhenFailed()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<SaveWhatsAppPhoneNumbersCommand>(), default))
            .ReturnsAsync((false, "Error"));

        var result = await _controller.SavePhoneNumbers(new List<MessageFlow.Server.DataTransferObjects.Client.PhoneNumberInfoDTO>());

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Error", bad.Value);
    }
}