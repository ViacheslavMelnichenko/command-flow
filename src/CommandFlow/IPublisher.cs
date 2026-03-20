namespace CommandFlow;

/// <summary>
/// Publishes a notification to multiple handlers.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// Handlers are invoked sequentially in registration order.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the publish operation.</returns>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}

