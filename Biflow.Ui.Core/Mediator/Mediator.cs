using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Ui.Core;

/// <summary>
/// Custom implementation of a dispatcher in the mediator pattern.
/// This type is registered as a scoped service in DI.
/// </summary>
internal class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        // Get the corresponding handler for the request.
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(request.GetType());
        var handler = (IRequestHandler)_serviceProvider.GetRequiredService(handlerType);
        return (Task)handler.Handle(request, cancellationToken);
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Get the corresponding handler for the request.
        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = (IRequestHandler)_serviceProvider.GetRequiredService(handlerType);
        return (Task<TResponse>)handler.Handle(request, cancellationToken);
    }
}
