# Notification

Notifications are published to all registered handlers.

## Define the notification

```csharp
using CommandFlow;

public record OrderPlacedEvent(int OrderId, string CustomerEmail) : INotification;
```

## Define a handler

```csharp
public class SendOrderConfirmationEmail : INotificationHandler<OrderPlacedEvent>
{
    private readonly IEmailService _email;

    public SendOrderConfirmationEmail(IEmailService email)
    {
        _email = email;
    }

    public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        await _email.SendAsync(
            notification.CustomerEmail,
            "Order Confirmed",
            $"Your order #{notification.OrderId} has been placed.",
            cancellationToken);
    }
}
```

## Publish

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new OrderPlacedEvent(42, "alice@example.com"));
```

## Execution strategy

Notification handlers execute **sequentially** in registration order:

- Each handler runs to completion before the next one starts
- If a handler throws, remaining handlers are **not** invoked
- The exception propagates to the caller

This is the simplest and most predictable execution strategy. It avoids the complexity of parallel execution, partial failure handling, and resource contention.

## Notes

- If no handlers are registered for a notification type, `Publish` completes successfully without error
- You can publish via `IPublisher` or `IMediator` (which implements `IPublisher`)

