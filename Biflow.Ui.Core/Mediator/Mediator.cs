using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Ui.Core;

internal class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();
        return handler.Handle(request, cancellationToken);
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
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
