namespace SourceGenMediator.Abstractions;

public interface INotificationPublisher
{
    ValueTask Publish<TNotification>(IReadOnlyList<Func<TNotification, CancellationToken, ValueTask>> handlers, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification;
}