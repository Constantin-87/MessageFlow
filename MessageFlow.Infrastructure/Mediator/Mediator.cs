using MessageFlow.Infrastructure.Mediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.Infrastructure.Mediator
{
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
            var handler = _serviceProvider.GetRequiredService(handlerType);
            return await (Task<TResponse>)handlerType.GetMethod("Handle").Invoke(handler, new object[] { request, cancellationToken });
        }
    }
}
