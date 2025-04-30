using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MessageFlow.DataAccess.Services;
using Microsoft.Extensions.Logging;

namespace MessageFlow.Tests.Helpers
{
    public static class TestMediatorFactory
    {
        public static (IMediator Mediator, Mock<IUnitOfWork> UnitOfWorkMock, Mock<ILogger<ProcessMessageHandler>> LoggerMock) CreateMediatorWithMocks()
        {
            var services = new ServiceCollection();

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var loggerMock = new Mock<ILogger<ProcessMessageHandler>>();

            services.AddSingleton<IUnitOfWork>(unitOfWorkMock.Object);
            services.AddSingleton(typeof(ILogger<ProcessMessageHandler>), loggerMock.Object);

            // Register handlers
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<ProcessMessageHandler>();
            });

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            return (mediator, unitOfWorkMock, loggerMock);
        }
    }
}