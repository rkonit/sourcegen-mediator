using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using SourceGenMediator.Abstractions;
using SourceGenMediator.DependencyInjection;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

BenchmarkRunner.Run<MediatorBenchmarks>();

// ---------------------------------------------------------------------------
// Shared request/handler types
// ---------------------------------------------------------------------------

public sealed record PingQuery(string Value) : SourceGenMediator.Abstractions.IRequest<string>,
                                                global::MediatR.IRequest<string>;

// SourceGenMediator handler
public sealed class FastPingHandler : SourceGenMediator.Abstractions.IRequestHandler<PingQuery, string>
{
    public ValueTask<string> Handle(PingQuery request, CancellationToken cancellationToken)
        => ValueTask.FromResult(request.Value);
}

// MediatR handler
public sealed class MediatRPingHandler : global::MediatR.IRequestHandler<PingQuery, string>
{
    public Task<string> Handle(PingQuery request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}

// Notification types
public sealed record PingNotification(string Value) : SourceGenMediator.Abstractions.INotification,
                                                       global::MediatR.INotification;

public sealed class FastNotificationHandler : SourceGenMediator.Abstractions.INotificationHandler<PingNotification>
{
    public ValueTask Handle(PingNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class MediatRNotificationHandler : global::MediatR.INotificationHandler<PingNotification>
{
    public Task Handle(PingNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

// Pipeline behavior types
public sealed class FastLoggingBehavior<TRequest, TResponse> : SourceGenMediator.Abstractions.IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public ValueTask<TResponse> Handle(TRequest request, SourceGenMediator.Abstractions.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        => next();
}

public sealed class MediatRLoggingBehavior<TRequest, TResponse> : global::MediatR.IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, global::MediatR.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        => next();
}

// ---------------------------------------------------------------------------
// Benchmark class
// ---------------------------------------------------------------------------

[MemoryDiagnoser]
[RankColumn]
public class MediatorBenchmarks
{
    private SourceGenMediator.Abstractions.IMediator _sourceGenMediator = null!;
    private global::MediatR.IMediator _mediatR = null!;
    private SourceGenMediator.Abstractions.IMediator _sourceGenMediatorWithBehavior = null!;
    private global::MediatR.IMediator _mediatRWithBehavior = null!;

    private ServiceProvider _sourceGenMediatorSp = null!;
    private ServiceProvider _mediatRSp = null!;
    private ServiceProvider _sourceGenMediatorWithBehaviorSp = null!;
    private ServiceProvider _mediatRWithBehaviorSp = null!;

    private static readonly PingQuery Query = new("ping");
    private static readonly PingNotification Notification = new("ping");

    [GlobalSetup]
    public void Setup()
    {
        // SourceGenMediator (no behavior)
        var fastServices = new ServiceCollection();
        fastServices.AddSourceGenMediatorGenerated();
        _sourceGenMediatorSp = fastServices.BuildServiceProvider();
        _sourceGenMediator = _sourceGenMediatorSp.GetRequiredService<SourceGenMediator.Abstractions.IMediator>();

        // MediatR (no behavior)
        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatRPingHandler>());
        _mediatRSp = mediatRServices.BuildServiceProvider();
        _mediatR = _mediatRSp.GetRequiredService<global::MediatR.IMediator>();

        // SourceGenMediator (with 1 pipeline behavior)
        var fastWithBehaviorServices = new ServiceCollection();
        fastWithBehaviorServices.AddSourceGenMediatorGenerated();
        fastWithBehaviorServices.AddTransient(typeof(SourceGenMediator.Abstractions.IPipelineBehavior<,>), typeof(FastLoggingBehavior<,>));
        _sourceGenMediatorWithBehaviorSp = fastWithBehaviorServices.BuildServiceProvider();
        _sourceGenMediatorWithBehavior = _sourceGenMediatorWithBehaviorSp.GetRequiredService<SourceGenMediator.Abstractions.IMediator>();

        // MediatR (with 1 pipeline behavior)
        var mediatRWithBehaviorServices = new ServiceCollection();
        mediatRWithBehaviorServices.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<MediatRPingHandler>();
            cfg.AddBehavior(typeof(global::MediatR.IPipelineBehavior<,>), typeof(MediatRLoggingBehavior<,>));
        });
        _mediatRWithBehaviorSp = mediatRWithBehaviorServices.BuildServiceProvider();
        _mediatRWithBehavior = _mediatRWithBehaviorSp.GetRequiredService<global::MediatR.IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _sourceGenMediatorSp?.Dispose();
        _mediatRSp?.Dispose();
        _sourceGenMediatorWithBehaviorSp?.Dispose();
        _mediatRWithBehaviorSp?.Dispose();
    }

    // ---- Send (request/response) ----------------------------------------

    [Benchmark(Description = "SourceGenMediator: Send")]
    public ValueTask<string> SourceGenMediator_Send()
        => _sourceGenMediator.Send(Query);

    [Benchmark(Description = "MediatR: Send", Baseline = true)]
    public Task<string> MediatR_Send()
        => _mediatR.Send(Query);

    // ---- Send with pipeline behavior ------------------------------------

    [Benchmark(Description = "SourceGenMediator: Send + 1 behavior")]
    public ValueTask<string> SourceGenMediator_Send_WithBehavior()
        => _sourceGenMediatorWithBehavior.Send(Query);

    [Benchmark(Description = "MediatR: Send + 1 behavior")]
    public Task<string> MediatR_Send_WithBehavior()
        => _mediatRWithBehavior.Send(Query);

    // ---- Publish (notifications) ----------------------------------------

    [Benchmark(Description = "SourceGenMediator: Publish")]
    public ValueTask SourceGenMediator_Publish()
        => _sourceGenMediator.Publish(Notification);

    [Benchmark(Description = "MediatR: Publish")]
    public Task MediatR_Publish()
        => _mediatR.Publish(Notification);
}