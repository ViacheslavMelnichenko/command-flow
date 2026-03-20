# Assembly Scanning

CommandFlow automatically discovers and registers handlers from assemblies you specify.

## How it works

When you call `AddCommandFlow`, the library scans each provided assembly for types that implement:

- `IRequestHandler<TRequest, TResponse>` — request/command/query handlers
- `INotificationHandler<TNotification>` — notification handlers

Only concrete, non-abstract, non-generic types are registered.

## Usage

### Single assembly

```csharp
services.AddCommandFlow(typeof(SomeHandler).Assembly);
```

### Multiple assemblies

```csharp
services.AddCommandFlow(
    typeof(Handlers.CreateUser).Assembly,
    typeof(Queries.GetUsers).Assembly
);
```

### Adding assemblies after initial registration

```csharp
services.AddCommandFlow(typeof(CoreHandler).Assembly)
    .AddHandlersFromAssemblies(typeof(PluginHandler).Assembly);
```

## What gets scanned

| Interface | Registered as |
|---|---|
| `IRequestHandler<TRequest, TResponse>` | Transient |
| `ICommandHandler<TCommand>` (implements `IRequestHandler<TCommand, Unit>`) | Transient |
| `INotificationHandler<TNotification>` | Transient |

## What is NOT scanned

- **Pipeline behaviors** are not auto-scanned. Register them explicitly with `.AddBehavior()`.
- **Open generic handlers** are not scanned (they are uncommon and require explicit registration).

This is by design: behaviors have an explicit execution order, so they should be registered explicitly.

## Example

Given this assembly:

```csharp
// All of these will be discovered automatically
public class CreateUserHandler : IRequestHandler<CreateUserCommand, int> { ... }
public class DeleteUserHandler : ICommandHandler<DeleteUserCommand> { ... }
public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto> { ... }
public class UserCreatedHandler : INotificationHandler<UserCreatedEvent> { ... }
public class AuditLogHandler : INotificationHandler<UserCreatedEvent> { ... }
```

Registration:

```csharp
services.AddCommandFlow(typeof(CreateUserHandler).Assembly);
```

This registers all five handlers automatically.

