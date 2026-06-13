namespace SourceGenMediator.Abstractions;

public interface IStreamPipelineBehavior<TRequest, TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}