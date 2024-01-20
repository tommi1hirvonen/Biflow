namespace Biflow.Ui.Core;

public interface IRequestHandler<TRequest> where TRequest : IRequest
{
    public Task Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}