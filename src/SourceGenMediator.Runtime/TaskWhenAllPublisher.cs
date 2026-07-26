using SourceGenMediator.Abstractions;

namespace SourceGenMediator.Runtime;

public sealed class TaskWhenAllPublisher : INotificationPublisher
{
    public async ValueTask Publish<TNotification>(IReadOnlyList<Func<TNotification, CancellationToken, ValueTask>> handlers, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (handlers.Count == 0)
        {
            return;
        }

        if (handlers.Count == 1)
        {
            await handlers[0](notification, cancellationToken).ConfigureAwait(false);
            return;
        }

        var tasks = new Task[handlers.Count];
        for (var index = 0; index < handlers.Count; index++)
        {
            tasks[index] = handlers[index](notification, cancellationToken).AsTask();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}