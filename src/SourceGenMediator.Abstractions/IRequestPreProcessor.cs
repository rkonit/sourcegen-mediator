namespace SourceGenMediator.Abstractions;

public interface IRequestPreProcessor<in TRequest>
{
    ValueTask Process(TRequest request, CancellationToken cancellationToken);
}