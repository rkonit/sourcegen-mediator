namespace SourceGenMediator.Abstractions;

public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestHandler<in TRequest> where TRequest : IRequest
{
    ValueTask<Unit> Handle(TRequest request, CancellationToken cancellationToken);
}