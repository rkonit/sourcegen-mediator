using System.Linq;

using SourceGenMediator.Abstractions;
using SourceGenMediator.Runtime;

using Microsoft.Extensions.DependencyInjection;

namespace SourceGenMediator.Tests;

public class GeneratedMediatorTests
{
    [Fact]
    public async Task Send_Dispatches_To_Correct_Handler()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<Ping, string>, PingHandler>();
        services.AddSingleton(new MediatorDispatchMap
        {
            RequestDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object?>>>
            {
                [typeof(Ping)] = static async (sp, request, ct) => await sp.GetRequiredService<IRequestHandler<Ping, string>>().Handle((Ping)request, ct)
            },
            NotificationDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>[]>(),
            StreamDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>()
        });
        services.AddSingleton<IMediator, GeneratedMediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new Ping("hello"));

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task Send_Throws_InvalidOperationException_For_Missing_Handler()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MediatorDispatchMap
        {
            RequestDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object?>>>(),
            NotificationDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>[]>(),
            StreamDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>()
        });
        services.AddSingleton<IMediator, GeneratedMediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new Ping("hello")).AsTask());
    }

    [Fact]
    public async Task Send_Throws_ArgumentNullException_For_Null_Request()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MediatorDispatchMap
        {
            RequestDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object?>>>(),
            NotificationDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>[]>(),
            StreamDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>()
        });
        services.AddSingleton<IMediator, GeneratedMediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send<string>(null!).AsTask());
    }

    [Fact]
    public async Task Publish_Dispatches_To_All_Notification_Handlers()
    {
        var callOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(callOrder);
        services.AddSingleton(new MediatorDispatchMap
        {
            RequestDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object?>>>(),
            NotificationDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>[]>
            {
                [typeof(OrderPlaced)] =
                [
                    static (sp, n, ct) =>
                    {
                        sp.GetRequiredService<List<string>>().Add("email");
                        return ValueTask.CompletedTask;
                    },
                    static (sp, n, ct) =>
                    {
                        sp.GetRequiredService<List<string>>().Add("log");
                        return ValueTask.CompletedTask;
                    }
                ]
            },
            StreamDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>()
        });
        services.AddSingleton<IMediator, GeneratedMediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Publish(new OrderPlaced("ORD-1"));

        Assert.Equal(2, callOrder.Count);
        Assert.Contains("email", callOrder);
        Assert.Contains("log", callOrder);
    }

    [Fact]
    public async Task Publish_With_No_Handlers_Does_Not_Throw()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MediatorDispatchMap
        {
            RequestDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object?>>>(),
            NotificationDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>[]>(),
            StreamDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>()
        });
        services.AddSingleton<IMediator, GeneratedMediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Should complete without throwing
        await mediator.Publish(new OrderPlaced("ORD-2"));
    }

    [Fact]
    public async Task CreateStream_Dispatches_To_Correct_Handler()
    {
        var services = new ServiceCollection();
        services.AddTransient<IStreamRequestHandler<NumbersQuery, int>, NumbersHandler>();
        services.AddSingleton(new MediatorDispatchMap
        {
            RequestDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object?>>>(),
            NotificationDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>[]>(),
            StreamDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>
            {
                [typeof(NumbersQuery)] = static (sp, request, ct) =>
                    (object)sp.GetRequiredService<IStreamRequestHandler<NumbersQuery, int>>().Handle((NumbersQuery)request, ct)
            }
        });
        services.AddSingleton<IMediator, GeneratedMediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var results = new List<int>();
        await foreach (var item in mediator.CreateStream(new NumbersQuery(3)))
        {
            results.Add(item);
        }

        Assert.Equal([1, 2, 3], results);
    }

    [Fact]
    public void CreateStream_Throws_InvalidOperationException_For_Missing_Handler()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MediatorDispatchMap
        {
            RequestDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object?>>>(),
            NotificationDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>[]>(),
            StreamDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>()
        });
        services.AddSingleton<IMediator, GeneratedMediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.Throws<InvalidOperationException>(() => mediator.CreateStream(new NumbersQuery(3)));
    }

    [Fact]
    public void CreateStream_Throws_ArgumentNullException_For_Null_Request()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MediatorDispatchMap
        {
            RequestDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object?>>>(),
            NotificationDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>[]>(),
            StreamDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>()
        });
        services.AddSingleton<IMediator, GeneratedMediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.Throws<ArgumentNullException>(() => mediator.CreateStream<int>(null!));
    }

    private sealed record NumbersQuery(int Count) : IStreamRequest<int>;

    private sealed class NumbersHandler : IStreamRequestHandler<NumbersQuery, int>
    {
        public async IAsyncEnumerable<int> Handle(NumbersQuery request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                yield return i;
                await Task.Yield();
            }
        }
    }

    [Fact]
    public async Task Pipeline_Behavior_Is_Invoked()
    {
        var invoked = false;
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<Ping, string>, PingHandler>();
        services.AddTransient<IPipelineBehavior<Ping, string>>(_ => new RecordingBehavior<Ping, string>(_ => invoked = true));
        services.AddSingleton(BuildDispatchMapWithPing());
        services.AddSingleton<IMediator, GeneratedMediator>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        await mediator.Send(new Ping("hello"));

        Assert.True(invoked);
    }

    [Fact]
    public async Task Pipeline_Behaviors_Execute_In_Registration_Order()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<Ping, string>, PingHandler>();
        services.AddTransient<IPipelineBehavior<Ping, string>>(_ => new RecordingBehavior<Ping, string>(_ => log.Add("first")));
        services.AddTransient<IPipelineBehavior<Ping, string>>(_ => new RecordingBehavior<Ping, string>(_ => log.Add("second")));
        services.AddSingleton(BuildDispatchMapWithPing());
        services.AddSingleton<IMediator, GeneratedMediator>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        await mediator.Send(new Ping("hello"));

        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public async Task Pipeline_Behavior_Can_Short_Circuit()
    {
        var handlerInvoked = false;
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<Ping, string>>(_ => new RecordingHandler(_ => handlerInvoked = true));
        services.AddTransient<IPipelineBehavior<Ping, string>>(_ => new ShortCircuitBehavior<Ping, string>("short-circuited"));
        services.AddSingleton(BuildDispatchMapWithPing());
        services.AddSingleton<IMediator, GeneratedMediator>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        var result = await mediator.Send(new Ping("hello"));

        Assert.Equal("short-circuited", result);
        Assert.False(handlerInvoked);
    }

    private static MediatorDispatchMap BuildDispatchMapWithPing() => new()
    {
        RequestDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object?>>>
        {
            [typeof(Ping)] = static async (sp, request, ct) =>
            {
                var typedRequest = (Ping)request;
                RequestHandlerDelegate<string> pipeline = () => sp.GetRequiredService<IRequestHandler<Ping, string>>().Handle(typedRequest, ct);
                foreach (var behavior in sp.GetServices<IPipelineBehavior<Ping, string>>().Reverse())
                {
                    var b = behavior;
                    var n = pipeline;
                    pipeline = () => b.Handle(typedRequest, n, ct);
                }
                return await pipeline().ConfigureAwait(false);
            }
        },
        NotificationDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>[]>(),
        StreamDispatchers = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>()
    };

    private sealed class RecordingBehavior<TRequest, TResponse>(Action<TRequest> onInvoke) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            onInvoke(request);
            return await next();
        }
    }

    private sealed class ShortCircuitBehavior<TRequest, TResponse>(TResponse result) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            => ValueTask.FromResult(result);
    }

    private sealed class RecordingHandler(Action<Ping> onInvoke) : IRequestHandler<Ping, string>
    {
        public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        {
            onInvoke(request);
            return ValueTask.FromResult(request.Value);
        }
    }

    private sealed record Ping(string Value) : IRequest<string>;

    private sealed class PingHandler : IRequestHandler<Ping, string>
    {
        public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
            => ValueTask.FromResult(request.Value);
    }

    private sealed record OrderPlaced(string OrderId) : INotification;

    private sealed class OrderPlacedEmailHandler(List<string> callOrder) : INotificationHandler<OrderPlaced>
    {
        public ValueTask Handle(OrderPlaced notification, CancellationToken cancellationToken)
        {
            callOrder.Add("email");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class OrderPlacedLogHandler(List<string> callOrder) : INotificationHandler<OrderPlaced>
    {
        public ValueTask Handle(OrderPlaced notification, CancellationToken cancellationToken)
        {
            callOrder.Add("log");
            return ValueTask.CompletedTask;
        }
    }
}