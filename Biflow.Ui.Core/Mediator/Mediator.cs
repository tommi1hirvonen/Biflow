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
        // Create a generic MethodInfo to be used with the request handler from DI.
        var genericType = typeof(IRequestHandler<>).MakeGenericType(request.GetType());
        var handleMethod = genericType.GetMethod(nameof(IRequestHandler<TRequest>.Handle));
        ArgumentNullException.ThrowIfNull(handleMethod);

        // Get the corresponding handler for the request.
        var handler = _serviceProvider.GetRequiredService(genericType);
        var task = handleMethod.Invoke(handler, parameters: [request, cancellationToken]);
        ArgumentNullException.ThrowIfNull(task);

        return (Task)task;
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Create a generic MethodInfo to be used with the request handler from DI.
        var genericType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));
        var handleMethod = genericType
            .GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle));
        ArgumentNullException.ThrowIfNull(handleMethod);

        // Get the corresponding handler for the request.
        var handler = _serviceProvider.GetRequiredService(genericType);
        var task = handleMethod.Invoke(handler, parameters: [request, cancellationToken]);
        ArgumentNullException.ThrowIfNull(task);
        
        return (Task<TResponse>)task;
    }
}
