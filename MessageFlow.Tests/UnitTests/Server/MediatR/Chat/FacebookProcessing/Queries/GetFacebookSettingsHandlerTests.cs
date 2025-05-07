using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Queries;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.QueryHandlers;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.FacebookProcessing.Queries
{
    public class GetFacebookSettingsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IAuthorizationHelper> _authMock = new();
        private readonly Mock<ILogger<GetFacebookSettingsHandler>> _loggerMock = new();
        private readonly GetFacebookSettingsHandler _handler;

        public GetFacebookSettingsHandlerTests()
        {
            _handler = new GetFacebookSettingsHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _authMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_Authorized_ReturnsMappedDto()
        {
            // Arrange
            var query = new GetFacebookSettingsQuery("company1");
            var model = new FacebookSettingsModel { Id = "f1", PageId = "p1", AccessToken = "token", CompanyId = "company1" };
            var dto = new FacebookSettingsDTO { Id = "f1", PageId = "p1", AccessToken = "token", CompanyId = "company1" };

            _authMock.Setup(a => a.ChannelSettingsAccess("company1"))
                .ReturnsAsync((true, string.Empty));

            _unitOfWorkMock.Setup(u => u.FacebookSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync(model);

            _mapperMock.Setup(m => m.Map<FacebookSettingsDTO>(model))
                .Returns(dto);

            // Act
            var result = await _handler.Handle(query, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("f1", result!.Id);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsNull()
        {
            // Arrange
            var query = new GetFacebookSettingsQuery("unauthorized-company");

            _authMock.Setup(a => a.ChannelSettingsAccess("unauthorized-company"))
                .ReturnsAsync((false, "Access denied"));

            // Act
            var result = await _handler.Handle(query, default);

            // Assert
            Assert.Null(result);
            _loggerMock.VerifyLog(LogLevel.Warning, Times.Once());
        }
    }

    internal static class LoggerExtensions
    {
        public static void VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel level, Times times)
        {
            logger.Verify(x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), times);
        }
    }
}
