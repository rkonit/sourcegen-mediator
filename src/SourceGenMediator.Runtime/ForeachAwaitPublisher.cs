using SourceGenMediator.Abstractions;

namespace SourceGenMediator.Runtime;

public sealed class ForeachAwaitPublisher : INotificationPublisher
{
    public async ValueTask Publish<TNotification>(IReadOnlyList<Func<TNotification, CancellationToken, ValueTask>> handlers, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        for (var index = 0; index < handlers.Count; index++)
        {
            await handlers[index](notification, cancellationToken).ConfigureAwait(false);
        }
    }
}