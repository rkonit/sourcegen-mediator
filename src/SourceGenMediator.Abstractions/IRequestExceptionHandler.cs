namespace SourceGenMediator.Abstractions;

public interface IRequestExceptionHandler<in TRequest, TResponse>
{
    ValueTask<ExceptionHandlingResult<TResponse>> Handle(TRequest request, Exception exception, CancellationToken cancellationToken);
}