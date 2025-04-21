using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.DataTransferObjects.Client;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.CommandHandlers;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.FacebookProcessing.Commands
{
    public class SaveFacebookSettingsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<ILogger<SaveFacebookSettingsHandler>> _loggerMock = new();
        private readonly Mock<IAuthorizationHelper> _authMock = new();

        private readonly SaveFacebookSettingsHandler _handler;

        public SaveFacebookSettingsHandlerTests()
        {
            _handler = new SaveFacebookSettingsHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _authMock.Object);
        }

        [Fact]
        public async Task Handle_InvalidCompanyId_ReturnsFalse()
        {
            var result = await _handler.Handle(new SaveFacebookSettingsCommand(
                "",
                new FacebookSettingsDTO
                {
                    Id = "dummy",
                    PageId = "pg1",
                    AccessToken = "tok1",
                    CompanyId = ""
                }), default);

            Assert.False(result.success);
            Assert.Equal("Invalid CompanyId provided.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsFalse()
        {
            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                     .ReturnsAsync((false, "Not allowed"));

            var result = await _handler.Handle(new SaveFacebookSettingsCommand("company1", new FacebookSettingsDTO()), default);

            Assert.False(result.success);
            Assert.Equal("Not allowed", result.errorMessage);
        }

        [Fact]
        public async Task Handle_AddsNewSettings_WhenNoneExist()
        {
            var dto = new FacebookSettingsDTO { PageId = "p", AccessToken = "token" };
            var model = new FacebookSettingsModel { Id = "new", CompanyId = "company1", PageId = "p", AccessToken = "token" };

            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                     .ReturnsAsync((true, string.Empty));
            _unitOfWorkMock.Setup(u => u.FacebookSettings.GetSettingsByCompanyIdAsync("company1"))
                           .ReturnsAsync((FacebookSettingsModel?)null);
            _mapperMock.Setup(m => m.Map<FacebookSettingsModel>(dto)).Returns(model);

            var result = await _handler.Handle(new SaveFacebookSettingsCommand("company1", dto), default);

            _unitOfWorkMock.Verify(u => u.FacebookSettings.AddEntityAsync(It.Is<FacebookSettingsModel>(s => s.CompanyId == "company1")), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            Assert.True(result.success);
        }

        [Fact]
        public async Task Handle_UpdatesExistingSettings()
        {
            var dto = new FacebookSettingsDTO { PageId = "updatedPage", AccessToken = "updatedToken" };
            var existing = new FacebookSettingsModel { Id = "123", CompanyId = "company1", PageId = "old", AccessToken = "old" };

            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                     .ReturnsAsync((true, string.Empty));
            _unitOfWorkMock.Setup(u => u.FacebookSettings.GetSettingsByCompanyIdAsync("company1"))
                           .ReturnsAsync(existing);

            var result = await _handler.Handle(new SaveFacebookSettingsCommand("company1", dto), default);

            Assert.Equal("updatedPage", existing.PageId);
            Assert.Equal("updatedToken", existing.AccessToken);
            _unitOfWorkMock.Verify(u => u.FacebookSettings.UpdateEntityAsync(existing), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            Assert.True(result.success);
        }

        [Fact]
        public async Task Handle_LogsErrorAndReturnsFalse_OnException()
        {
            _authMock.Setup(a => a.ChannelSettingsAccess("company1")).Throws(new Exception("fail"));

            var result = await _handler.Handle(new SaveFacebookSettingsCommand("company1", new FacebookSettingsDTO()), default);

            Assert.False(result.success);
            Assert.Equal("An error occurred while saving Facebook settings.", result.errorMessage);
        }
    }
}
