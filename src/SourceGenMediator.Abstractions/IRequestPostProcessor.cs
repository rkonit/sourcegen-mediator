namespace SourceGenMediator.Abstractions;

public interface IRequestPostProcessor<in TRequest, in TResponse>
{
    ValueTask Handle(TRequest request, TResponse response, CancellationToken cancellationToken);
}