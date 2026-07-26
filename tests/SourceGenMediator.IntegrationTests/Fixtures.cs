using SourceGenMediator.Abstractions;

namespace SourceGenMediator.IntegrationTests;

// Void command (IRequest without response).
public sealed record VoidDoThing(string Value) : ICommand;

public sealed class VoidDoThingHandler : ICommandHandler<VoidDoThing>
{
    public static readonly List<string> Calls = new();

    public ValueTask<Unit> Handle(VoidDoThing command, CancellationToken cancellationToken)
    {
        Calls.Add(command.Value);
        return Unit.Task;
    }
}

// Sender / publisher split.
public sealed record SenderPing(string Value) : ICommand<string>;

public sealed class SenderPingHandler : ICommandHandler<SenderPing, string>
{
    public ValueTask<string> Handle(SenderPing command, CancellationToken cancellationToken)
        => ValueTask.FromResult(command.Value);
}

public sealed record SenderDone(string Value) : INotification;

public sealed class SenderDoneHandler : INotificationHandler<SenderDone>
{
    public static bool Called;

    public ValueTask Handle(SenderDone notification, CancellationToken cancellationToken)
    {
        Called = true;
        return ValueTask.CompletedTask;
    }
}

// Pre / post processors.
public sealed record PrePostPing(string Value) : ICommand<string>;

public sealed class PrePostPingHandler : ICommandHandler<PrePostPing, string>
{
    public ValueTask<string> Handle(PrePostPing command, CancellationToken cancellationToken)
        => ValueTask.FromResult(command.Value);
}

public sealed class PrePostPre : IRequestPreProcessor<PrePostPing>
{
    public static readonly List<string> Calls = new();

    public ValueTask Process(PrePostPing request, CancellationToken cancellationToken)
    {
        Calls.Add("pre:" + request.Value);
        return ValueTask.CompletedTask;
    }
}

public sealed class PrePostPost : IRequestPostProcessor<PrePostPing, string>
{
    public ValueTask Handle(PrePostPing request, string response, CancellationToken cancellationToken)
    {
        PrePostPre.Calls.Add("post:" + response);
        return ValueTask.CompletedTask;
    }
}

// Exception handler returning a fallback response.
public sealed record ExFallbackPing(string Value) : ICommand<string>;

public sealed class ExFallbackPingHandler : ICommandHandler<ExFallbackPing, string>
{
    public ValueTask<string> Handle(ExFallbackPing command, CancellationToken cancellationToken)
        => throw new InvalidOperationException("boom");
}

public sealed class ExFallbackHandler : IRequestExceptionHandler<ExFallbackPing, string>
{
    public ValueTask<ExceptionHandlingResult<string>> Handle(ExFallbackPing request, Exception exception, CancellationToken cancellationToken)
        => ValueTask.FromResult(ExceptionHandlingResult<string>.HandledResult("fallback:" + request.Value));
}

// Exception action observing then rethrowing.
public sealed record ExObservePing(string Value) : ICommand<string>;

public sealed class ExObservePingHandler : ICommandHandler<ExObservePing, string>
{
    public ValueTask<string> Handle(ExObservePing command, CancellationToken cancellationToken)
        => throw new InvalidOperationException("boom");
}

public sealed class ExObserveAction : IRequestExceptionAction<ExObservePing>
{
    public static bool Called;

    public ValueTask Execute(ExObservePing request, Exception exception, CancellationToken cancellationToken)
    {
        Called = true;
        return ValueTask.CompletedTask;
    }
}

// Stream behavior wrapping the handler stream.
public sealed record StreamNumbers(int Count) : IStreamRequest<int>;

public sealed class StreamNumbersHandler : IStreamRequestHandler<StreamNumbers, int>
{
    public async IAsyncEnumerable<int> Handle(StreamNumbers request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 1; i <= request.Count; i++)
        {
            yield return i;
            await Task.Yield();
        }
    }
}

public sealed class StreamDoublingBehavior : IStreamPipelineBehavior<StreamNumbers, int>
{
    public async IAsyncEnumerable<int> Handle(StreamNumbers request, StreamHandlerDelegate<int> next, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in next().WithCancellation(cancellationToken))
        {
            yield return item * 2;
        }
    }
}

// Custom notification publisher.
public sealed record PubDone(string Value) : INotification;

public sealed class PubFirstHandler : INotificationHandler<PubDone>
{
    public static readonly List<string> Calls = new();

    public ValueTask Handle(PubDone notification, CancellationToken cancellationToken)
    {
        lock (Calls)
        {
            Calls.Add("first");
        }

        return ValueTask.CompletedTask;
    }
}

public sealed class PubSecondHandler : INotificationHandler<PubDone>
{
    public ValueTask Handle(PubDone notification, CancellationToken cancellationToken)
    {
        lock (PubFirstHandler.Calls)
        {
            PubFirstHandler.Calls.Add("second");
        }

        return ValueTask.CompletedTask;
    }
}
