using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Moq;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Tests
{
    public static class TestMediatorFactory
    {
        public static IMediator CreateMediatorWithMocks()
        {
            var services = new ServiceCollection();

            // Add Mediator and Handlers
            services.AddSingleton<IMediator, Mediator>();
            services.AddTransient<IRequestHandler<ProcessMessageCommand, Unit>, ProcessMessageHandler>();
            services.AddTransient<IRequestHandler<ProcessMessageStatusUpdateCommand, bool>, ProcessMessageStatusUpdateHandler>();

            return services.BuildServiceProvider().GetRequiredService<IMediator>();
        }
    }
}
