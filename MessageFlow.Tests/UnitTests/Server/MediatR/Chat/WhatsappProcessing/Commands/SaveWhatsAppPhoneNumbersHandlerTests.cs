using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.CommandHandlers;
using MessageFlow.Server.DataTransferObjects.Client;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.WhatsappProcessing.Commands
{
    public class SaveWhatsAppPhoneNumbersHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ILogger<SaveWhatsAppPhoneNumbersHandler>> _loggerMock = new();
        private readonly Mock<IAuthorizationHelper> _authMock = new();
        private readonly SaveWhatsAppPhoneNumbersHandler _handler;

        public SaveWhatsAppPhoneNumbersHandlerTests()
        {
            _handler = new SaveWhatsAppPhoneNumbersHandler(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _authMock.Object
            );
        }

        [Fact]
        public async Task Handle_InvalidCompanyId_ReturnsFalse()
        {
            var cmd = new SaveWhatsAppPhoneNumbersCommand(new List<PhoneNumberInfoDTO>());

            var result = await _handler.Handle(cmd, default);

            Assert.False(result.success);
            Assert.Equal("Invalid CompanyId provided.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsFalse()
        {
            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                .ReturnsAsync((false, "unauthorized"));

            var cmd = new SaveWhatsAppPhoneNumbersCommand(new List<PhoneNumberInfoDTO>
            {
                new() { PhoneNumber = "+111", PhoneNumberId = "pnid1", CompanyId = "company1" }
            });

            var result = await _handler.Handle(cmd, default);

            Assert.False(result.success);
            Assert.Equal("unauthorized", result.errorMessage);
        }

        [Fact]
        public async Task Handle_SettingsNotFound_ReturnsFalse()
        {
            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                .ReturnsAsync((true, string.Empty));

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync((WhatsAppSettingsModel?)null);

            var cmd = new SaveWhatsAppPhoneNumbersCommand(new List<PhoneNumberInfoDTO>
            {
                new() { PhoneNumber = "+111", PhoneNumberId = "pnid1", CompanyId = "company1" }
            });

            var result = await _handler.Handle(cmd, default);

            Assert.False(result.success);
            Assert.Equal("WhatsApp settings not found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_ValidRequest_SavesPhoneNumbers()
        {
            var existingSettings = new WhatsAppSettingsModel
            {
                Id = "wa1",
                CompanyId = "company1"
            };

            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                .ReturnsAsync((true, string.Empty));

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync(existingSettings);

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.UpdateEntityAsync(existingSettings))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var cmd = new SaveWhatsAppPhoneNumbersCommand(new List<PhoneNumberInfoDTO>
            {
                new() { PhoneNumber = "+111", PhoneNumberId = "pnid1", CompanyId = "company1" }
            });

            var result = await _handler.Handle(cmd, default);

            Assert.True(result.success);
            Assert.Single(existingSettings.PhoneNumbers);
            Assert.Equal("+111", existingSettings.PhoneNumbers.First().PhoneNumber);
        }
    }
}
