namespace SourceGenMediator.Abstractions;

public readonly struct ExceptionHandlingResult<TResponse>
{
    private ExceptionHandlingResult(bool handled, TResponse? response)
    {
        Handled = handled;
        Response = response;
    }

    public bool Handled { get; }

    public TResponse? Response { get; }

    public static ExceptionHandlingResult<TResponse> NotHandled => default;

    public static ExceptionHandlingResult<TResponse> HandledResult(TResponse response) => new(true, response);
}