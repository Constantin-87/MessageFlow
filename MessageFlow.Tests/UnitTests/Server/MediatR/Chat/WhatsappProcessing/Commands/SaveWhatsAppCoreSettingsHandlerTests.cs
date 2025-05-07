using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.CommandHandlers;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.WhatsappProcessing.Commands
{
    public class SaveWhatsAppCoreSettingsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ILogger<SaveWhatsAppCoreSettingsHandler>> _loggerMock = new();
        private readonly Mock<IAuthorizationHelper> _authMock = new();
        private readonly SaveWhatsAppCoreSettingsHandler _handler;

        public SaveWhatsAppCoreSettingsHandlerTests()
        {
            _handler = new SaveWhatsAppCoreSettingsHandler(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _authMock.Object
            );
        }

        [Fact]
        public async Task Handle_InvalidCompanyId_ReturnsFalse()
        {
            var cmd = new SaveWhatsAppCoreSettingsCommand { CompanyId = "", BusinessAccountId = "BID", AccessToken = "TOKEN" };

            var result = await _handler.Handle(cmd, default);

            Assert.False(result.success);
            Assert.Equal("Invalid CompanyId provided.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsFalse()
        {
            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                .ReturnsAsync((false, "unauthorized"));

            var cmd = new SaveWhatsAppCoreSettingsCommand
            {
                CompanyId = "company1",
                BusinessAccountId = "bid",
                AccessToken = "token"
            };

            var result = await _handler.Handle(cmd, default);

            Assert.False(result.success);
            Assert.Equal("unauthorized", result.errorMessage);
        }

        [Fact]
        public async Task Handle_NewSettings_SavesSuccessfully()
        {
            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                .ReturnsAsync((true, string.Empty));

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync((WhatsAppSettingsModel?)null);

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.AddEntityAsync(It.IsAny<WhatsAppSettingsModel>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var cmd = new SaveWhatsAppCoreSettingsCommand
            {
                CompanyId = "company1",
                BusinessAccountId = "BID",
                AccessToken = "TOKEN"
            };

            var result = await _handler.Handle(cmd, default);

            Assert.True(result.success);
        }

        [Fact]
        public async Task Handle_UpdatesExistingSettings()
        {
            var existing = new WhatsAppSettingsModel
            {
                Id = "w1",
                CompanyId = "company1",
                AccessToken = "old",
                BusinessAccountId = "old"
            };

            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                .ReturnsAsync((true, string.Empty));

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync(existing);

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.UpdateEntityAsync(existing))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var cmd = new SaveWhatsAppCoreSettingsCommand
            {
                CompanyId = "company1",
                BusinessAccountId = "NEWBID",
                AccessToken = "NEWTOKEN"
            };

            var result = await _handler.Handle(cmd, default);

            Assert.True(result.success);
            Assert.Equal("NEWBID", existing.BusinessAccountId);
            Assert.Equal("NEWTOKEN", existing.AccessToken);
        }
    }
}
