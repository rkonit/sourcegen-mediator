using SourceGenMediator.Abstractions;

namespace SourceGenMediator.Runtime;

public sealed class MediatorDispatchMap
{
    public required IReadOnlyDictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object?>>> RequestDispatchers { get; init; }
    public required IReadOnlyDictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>[]> NotificationDispatchers { get; init; }
    public required IReadOnlyDictionary<Type, Func<IServiceProvider, object, CancellationToken, object>> StreamDispatchers { get; init; }
}

public sealed class GeneratedMediator(IServiceProvider serviceProvider, MediatorDispatchMap dispatchMap) : IMediator
{
    public async ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        if (!dispatchMap.RequestDispatchers.TryGetValue(requestType, out var dispatcher))
        {
            throw new InvalidOperationException($"No request handler is registered for '{requestType.FullName}'.");
        }

        return (TResponse)(await dispatcher(serviceProvider, request, cancellationToken).ConfigureAwait(false))!;
    }

    public async ValueTask<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!dispatchMap.RequestDispatchers.TryGetValue(typeof(TRequest), out var dispatcher))
        {
            throw new InvalidOperationException($"No request handler is registered for '{typeof(TRequest).FullName}'.");
        }

        return (TResponse)(await dispatcher(serviceProvider, request, cancellationToken).ConfigureAwait(false))!;
    }

    public async ValueTask<Unit> Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        if (!dispatchMap.RequestDispatchers.TryGetValue(requestType, out var dispatcher))
        {
            throw new InvalidOperationException($"No request handler is registered for '{requestType.FullName}'.");
        }

        return (Unit)(await dispatcher(serviceProvider, request, cancellationToken).ConfigureAwait(false))!;
    }

    public async ValueTask Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!dispatchMap.RequestDispatchers.TryGetValue(typeof(TRequest), out var dispatcher))
        {
            throw new InvalidOperationException($"No request handler is registered for '{typeof(TRequest).FullName}'.");
        }

        await dispatcher(serviceProvider, request, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (!dispatchMap.NotificationDispatchers.TryGetValue(typeof(TNotification), out var dispatchers))
        {
            return;
        }

        if (dispatchers.Length == 0)
        {
            return;
        }

        var publisher = serviceProvider.GetService(typeof(INotificationPublisher)) as INotificationPublisher;
        if (publisher is null)
        {
            for (var index = 0; index < dispatchers.Length; index++)
            {
                await dispatchers[index](serviceProvider, notification, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        var handlers = new Func<TNotification, CancellationToken, ValueTask>[dispatchers.Length];
        for (var index = 0; index < dispatchers.Length; index++)
        {
            var dispatcher = dispatchers[index];
            handlers[index] = (n, ct) => dispatcher(serviceProvider, n!, ct);
        }

        await publisher.Publish(handlers, notification, cancellationToken).ConfigureAwait(false);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        if (!dispatchMap.StreamDispatchers.TryGetValue(requestType, out var dispatcher))
        {
            throw new InvalidOperationException($"No stream handler is registered for '{requestType.FullName}'.");
        }

        return (IAsyncEnumerable<TResponse>)dispatcher(serviceProvider, request, cancellationToken);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IStreamRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!dispatchMap.StreamDispatchers.TryGetValue(typeof(TRequest), out var dispatcher))
        {
            throw new InvalidOperationException($"No stream handler is registered for '{typeof(TRequest).FullName}'.");
        }

        return (IAsyncEnumerable<TResponse>)dispatcher(serviceProvider, request, cancellationToken);
    }
}