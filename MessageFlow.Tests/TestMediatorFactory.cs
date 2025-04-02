using Microsoft.Extensions.DependencyInjection;
using MessageFlow.Infrastructure.Mediator;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Server.Services.Interfaces;
using Moq;
using MessageFlow.Server.MediatorComponents.Chat.Commands;
using MessageFlow.Server.MediatorComponents.Chat.CommandHandlers;

namespace MessageFlow.Tests
{
    public static class TestMediatorFactory
    {
        public static IMediator CreateMediatorWithMocks(
            Mock<IFacebookService>? facebookServiceMock = null,
            Mock<IWhatsAppService>? whatsappServiceMock = null,
            Mock<IMessageProcessingService>? messageProcessingServiceMock = null)
        {
            var services = new ServiceCollection();

            // Add mocked services
            facebookServiceMock ??= new Mock<IFacebookService>();
            whatsappServiceMock ??= new Mock<IWhatsAppService>();
            messageProcessingServiceMock ??= new Mock<IMessageProcessingService>();

            services.AddSingleton(facebookServiceMock.Object);
            services.AddSingleton(whatsappServiceMock.Object);
            services.AddSingleton(messageProcessingServiceMock.Object);

            // Add Mediator and Handlers
            services.AddSingleton<IMediator, Mediator>();
            services.AddTransient<IRequestHandler<SendFacebookMessageCommand, bool>, SendFacebookMessageHandler>();
            services.AddTransient<IRequestHandler<SendWhatsAppMessageCommand, bool>, SendWhatsAppMessageHandler>();
            services.AddTransient<IRequestHandler<ProcessMessageCommand, bool>, ProcessMessageHandler>();
            services.AddTransient<IRequestHandler<ProcessMessageStatusUpdateCommand, bool>, ProcessMessageStatusUpdateHandler>();

            return services.BuildServiceProvider().GetRequiredService<IMediator>();
        }
    }
}
