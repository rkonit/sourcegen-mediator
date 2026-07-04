namespace SourceGenMediator.Abstractions;

public interface IRequestExceptionAction<in TRequest>
{
    ValueTask Execute(TRequest request, Exception exception, CancellationToken cancellationToken);
}