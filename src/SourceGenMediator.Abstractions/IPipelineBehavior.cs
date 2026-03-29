namespace SourceGenMediator.Abstractions;

public interface IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}