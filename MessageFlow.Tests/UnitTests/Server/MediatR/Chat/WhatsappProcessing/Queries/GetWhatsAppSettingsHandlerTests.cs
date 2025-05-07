using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Queries;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.QueryHandlers;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.WhatsappProcessing.Queries
{
    public class GetWhatsAppSettingsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IAuthorizationHelper> _authMock = new();
        private readonly Mock<ILogger<GetWhatsAppSettingsHandler>> _loggerMock = new();
        private readonly GetWhatsAppSettingsHandler _handler;

        public GetWhatsAppSettingsHandlerTests()
        {
            _handler = new GetWhatsAppSettingsHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _authMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_UnauthorizedAccess_ReturnsNull()
        {
            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                .ReturnsAsync((false, "Not allowed"));

            var result = await _handler.Handle(new GetWhatsAppSettingsQuery("company1"), CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_Authorized_ReturnsMappedDto()
        {
            var settings = new WhatsAppSettingsModel { CompanyId = "company1" };
            var dto = new WhatsAppSettingsDTO { CompanyId = "company1" };

            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                .ReturnsAsync((true, string.Empty));
            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync(settings);
            _mapperMock.Setup(m => m.Map<WhatsAppSettingsDTO>(settings))
                .Returns(dto);

            var result = await _handler.Handle(new GetWhatsAppSettingsQuery("company1"), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("company1", result.CompanyId);
        }
    }
}
