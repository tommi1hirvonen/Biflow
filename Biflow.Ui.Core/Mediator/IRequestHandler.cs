﻿namespace Biflow.Ui.Core;

internal interface IRequestHandler<in TRequest> : IRequestHandler
    where TRequest : IRequest
{
    public Task Handle(TRequest request, CancellationToken cancellationToken);

    object IRequestHandler.Handle(object request, CancellationToken cancellationToken) =>
        Handle((TRequest)request, cancellationToken);
}

internal interface IRequestHandler<in TRequest, TResponse> : IRequestHandler
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);

    object IRequestHandler.Handle(object request, CancellationToken cancellationToken) =>
        Handle((TRequest)request, cancellationToken);
}

internal interface IRequestHandler
{
    public object Handle(object request, CancellationToken cancellationToken);
}