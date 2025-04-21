using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Moq;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Tests
{
    public static class TestMediatorFactory
    {
        public static IMediator CreateMediatorWithMocks(
            //Mock<IFacebookService>? facebookServiceMock = null,
            //Mock<IWhatsAppService>? whatsappServiceMock = null
            )
        {
            var services = new ServiceCollection();

            // Add mocked services
            //facebookServiceMock ??= new Mock<IFacebookService>();
            //whatsappServiceMock ??= new Mock<IWhatsAppService>();

            //services.AddSingleton(facebookServiceMock.Object);
            //services.AddSingleton(whatsappServiceMock.Object);

            // Add Mediator and Handlers
            services.AddSingleton<IMediator, Mediator>();
            //services.AddTransient<IRequestHandler<SendFacebookMessageCommand, bool>, SendFacebookMessageHandler>();
            //services.AddTransient<IRequestHandler<SendWhatsAppMessageCommand, bool>, SendWhatsAppMessageHandler>();
            services.AddTransient<IRequestHandler<ProcessMessageCommand, Unit>, ProcessMessageHandler>();
            services.AddTransient<IRequestHandler<ProcessMessageStatusUpdateCommand, bool>, ProcessMessageStatusUpdateHandler>();

            return services.BuildServiceProvider().GetRequiredService<IMediator>();
        }
    }
}
