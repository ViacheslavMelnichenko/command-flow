# Multiple Notification Handlers

A single notification can be handled by any number of handlers.

## Define the notification

```csharp
using CommandFlow;

public record UserRegisteredEvent(int UserId, string Email) : INotification;
```

## Define multiple handlers

```csharp
public class SendWelcomeEmail : INotificationHandler<UserRegisteredEvent>
{
    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Sending welcome email to {notification.Email}");
        return Task.CompletedTask;
    }
}

public class CreateDefaultSettings : INotificationHandler<UserRegisteredEvent>
{
    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Creating default settings for user {notification.UserId}");
        return Task.CompletedTask;
    }
}

public class LogUserRegistration : INotificationHandler<UserRegisteredEvent>
{
    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"User {notification.UserId} registered");
        return Task.CompletedTask;
    }
}
```

## Register and publish

All three handlers are automatically discovered by assembly scanning:

```csharp
services.AddCommandFlow(typeof(SendWelcomeEmail).Assembly);

// Later...
var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new UserRegisteredEvent(1, "alice@example.com"));
```

## Execution order

Handlers execute sequentially in the order they were registered. With assembly scanning, the order is determined by the runtime's type enumeration (typically declaration order within an assembly, but not guaranteed).

If you need a guaranteed order, register handlers explicitly:

```csharp
services.AddTransient<INotificationHandler<UserRegisteredEvent>, LogUserRegistration>();
services.AddTransient<INotificationHandler<UserRegisteredEvent>, CreateDefaultSettings>();
services.AddTransient<INotificationHandler<UserRegisteredEvent>, SendWelcomeEmail>();
```

