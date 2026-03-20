<p align="center">
  <img src="assets/icon.svg" alt="CommandFlow" width="120" />
</p>

<h1 align="center">CommandFlow</h1>

<p align="center">A lightweight in-process messaging and CQRS library for .NET.</p>

<p align="center">
  <a href="https://github.com/ViacheslavMelnichenko/command-flow/actions/workflows/ci.yml"><img src="https://github.com/ViacheslavMelnichenko/command-flow/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://www.nuget.org/packages/CommandFlow"><img src="https://img.shields.io/nuget/v/CommandFlow.svg" alt="NuGet"></a>
  <a href="https://github.com/ViacheslavMelnichenko/command-flow"><img src="https://img.shields.io/badge/coverage-100%25-brightgreen" alt="Coverage"></a>
</p>

## Overview

CommandFlow provides a clean, minimal implementation of the mediator pattern for .NET applications. It enables in-process messaging with support for commands, queries, notifications, and pipeline behaviors — all with first-class dependency injection support.

## Goals

- **Lightweight** — minimal dependencies, small API surface
- **Familiar** — intuitive for developers experienced with CQRS/mediator patterns
- **Production-ready** — nullable-aware, cancellation-aware, deterministic behavior ordering
- **Testable** — easy to mock, easy to verify

## Features

- ✅ Commands (void and with result)
- ✅ Queries (with result)
- ✅ Request/response pattern
- ✅ Notifications with multiple handlers
- ✅ Pipeline behaviors (middleware)
- ✅ Deterministic behavior execution order
- ✅ Microsoft.Extensions.DependencyInjection integration
- ✅ Assembly scanning for automatic handler registration
- ✅ Full CancellationToken flow
- ✅ Compatible with CSharpFunctionalExtensions Result types (no extra packages needed)

## Installation

```bash
dotnet add package CommandFlow
dotnet add package CommandFlow.DependencyInjection
```

## Quick Start

### 1. Define a request and handler

```csharp
using CommandFlow;

public record GetUserQuery(int UserId) : IQuery<UserDto>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = new UserDto(request.UserId, "Alice");
        return Task.FromResult(user);
    }
}

public record UserDto(int Id, string Name);
```

### 2. Register services

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCommandFlow(typeof(GetUserQueryHandler).Assembly);
```

### 3. Send a request

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();
var user = await mediator.Send(new GetUserQuery(1));
```

## Commands

### Void command (no return value)

```csharp
public record DeleteUserCommand(int UserId) : ICommand;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    public Task HandleCommand(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        // Delete user logic
        return Task.CompletedTask;
    }
}

// Usage
await mediator.Send(new DeleteUserCommand(1));
```

### Command with result

```csharp
public record CreateUserCommand(string Name) : ICommand<int>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    public Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Create user, return ID
        return Task.FromResult(42);
    }
}

// Usage
int userId = await mediator.Send(new CreateUserCommand("Alice"));
```

## Queries

```csharp
public record GetOrdersQuery(int UserId) : IQuery<List<OrderDto>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    public Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = new List<OrderDto> { new(1, "Widget") };
        return Task.FromResult(orders);
    }
}

public record OrderDto(int Id, string ProductName);
```

## Notifications

Notifications are dispatched to **all** registered handlers. Handlers execute **sequentially** in registration order. If a handler throws, subsequent handlers will not execute.

```csharp
public record OrderPlacedEvent(int OrderId) : INotification;

public class SendEmailOnOrderPlaced : INotificationHandler<OrderPlacedEvent>
{
    public Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        // Send confirmation email
        return Task.CompletedTask;
    }
}

public class UpdateInventoryOnOrderPlaced : INotificationHandler<OrderPlacedEvent>
{
    public Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        // Update inventory
        return Task.CompletedTask;
    }
}

// Usage
await mediator.Publish(new OrderPlacedEvent(42));
```

### Notification execution strategy

Notifications use **sequential execution** by design:

- Handlers are invoked one at a time in the order they were registered
- If a handler throws an exception, the remaining handlers are **not** executed
- The exception propagates to the caller
- This is the simplest and most predictable strategy

## Pipeline Behaviors

Pipeline behaviors wrap request handling, enabling cross-cutting concerns like validation, logging, caching, and performance monitoring.

Behaviors execute in **registration order**: the first registered behavior is the outermost wrapper.

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}
```

### Behavior execution order

```
Request → Behavior1 → Behavior2 → Handler → Behavior2 → Behavior1 → Response
```

Register behaviors in the order you want them to wrap:

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddBehavior(typeof(LoggingBehavior<,>))        // outermost
    .AddBehavior(typeof(ValidationBehavior<,>));     // inner
```

### Short-circuiting

A behavior can skip calling `next()` to short-circuit the pipeline:

```csharp
public class CacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check cache, return early if found
        // Otherwise call next() and cache the result
        return await next();
    }
}
```

## Result-Based Responses (CSharpFunctionalExtensions)

CommandFlow is fully compatible with [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions) Result types. No special integration package or abstractions are needed — `Result`, `Result<T>`, `UnitResult<TError>`, and `Result<TValue, TError>` are simply used as `TResponse`.

This works because CommandFlow's generics are open: `ICommand<TResult>`, `IQuery<TResult>`, `IRequestHandler<TRequest, TResponse>`, and `IPipelineBehavior<TRequest, TResponse>` accept any response type.

### When to use Result vs exceptions

| Scenario | Approach |
|---|---|
| Domain validation ("quantity must be positive") | `Result.Failure(...)` |
| Business rule violation ("insufficient funds") | `Result.Failure(...)` |
| Entity not found | `Result.Failure(...)` |
| Unexpected infrastructure error (database down) | Let the exception throw |
| Cancellation | `OperationCanceledException` (standard .NET) |

**Rule of thumb**: if the caller is expected to handle the failure as part of normal flow, return a `Result`. If the failure is unexpected and indicates a bug or infrastructure issue, throw.

### Command returning Result

```csharp
using CSharpFunctionalExtensions;

public record PlaceOrderCommand(string ProductName, int Quantity) : ICommand<Result>;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Result>
{
    public Task<Result> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            return Task.FromResult(Result.Failure("Quantity must be positive"));

        return Task.FromResult(Result.Success());
    }
}

var result = await mediator.Send(new PlaceOrderCommand("Widget", 3));
if (result.IsFailure)
    Console.WriteLine(result.Error);
```

### Command returning Result&lt;T&gt;

```csharp
public record CreateOrderCommand(string Name, int Qty) : ICommand<Result<int>>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<int>>
{
    public Task<Result<int>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Qty <= 0)
            return Task.FromResult(Result.Failure<int>("Quantity must be positive"));

        return Task.FromResult(Result.Success(42));
    }
}
```

### Query returning Result&lt;T&gt;

```csharp
public record FindOrderQuery(int OrderId) : IQuery<Result<OrderDto>>;

public class FindOrderHandler : IRequestHandler<FindOrderQuery, Result<OrderDto>>
{
    public Task<Result<OrderDto>> Handle(FindOrderQuery request, CancellationToken cancellationToken)
    {
        if (request.OrderId <= 0)
            return Task.FromResult(Result.Failure<OrderDto>("Invalid order ID"));

        return Task.FromResult(Result.Success(new OrderDto(request.OrderId, "Widget", 5)));
    }
}
```

### Typed errors with Result&lt;TValue, TError&gt;

```csharp
public record TransferError(string Code, string Message);
public record TransferFundsCommand(decimal Amount) : ICommand<Result<decimal, TransferError>>;

public class TransferHandler : IRequestHandler<TransferFundsCommand, Result<decimal, TransferError>>
{
    public Task<Result<decimal, TransferError>> Handle(
        TransferFundsCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return Task.FromResult(
                Result.Failure<decimal, TransferError>(
                    new TransferError("INVALID", "Amount must be positive")));

        return Task.FromResult(Result.Success<decimal, TransferError>(request.Amount));
    }
}
```

### UnitResult&lt;TError&gt;

```csharp
public record ArchiveOrderCommand(int OrderId) : ICommand<UnitResult<string>>;

public class ArchiveOrderHandler : IRequestHandler<ArchiveOrderCommand, UnitResult<string>>
{
    public Task<UnitResult<string>> Handle(ArchiveOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.OrderId <= 0)
            return Task.FromResult(UnitResult.Failure("Invalid order ID"));

        return Task.FromResult(UnitResult.Success<string>());
    }
}
```

### Pipeline behaviors work transparently

Generic behaviors compose with Result-based handlers without special handling. A `Result.Failure(...)` is a normal return value, not an exception — it flows through the pipeline like any other response:

```csharp
services.AddCommandFlow(typeof(PlaceOrderHandler).Assembly)
    .AddBehavior(typeof(LoggingBehavior<,>));
```

## Dependency Injection Setup

### Basic registration

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly);
```

### With pipeline behaviors

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddBehavior(typeof(LoggingBehavior<,>))
    .AddBehavior(typeof(ValidationBehavior<,>));
```

### Multiple assemblies

```csharp
services.AddCommandFlow(
    typeof(MyHandler).Assembly,
    typeof(AnotherHandler).Assembly
);
```

### Additional assembly scanning

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddHandlersFromAssemblies(typeof(PluginHandler).Assembly);
```

### What gets registered

| Service | Lifetime | Description |
|---------|----------|-------------|
| `IMediator` | Transient | Primary entry point (implements ISender + IPublisher) |
| `ISender` | Transient | Request dispatching only |
| `IPublisher` | Transient | Notification publishing only |
| `IRequestHandler<TRequest, TResponse>` | Transient | Request handlers (auto-scanned) |
| `INotificationHandler<TNotification>` | Transient | Notification handlers (auto-scanned) |
| `IPipelineBehavior<TRequest, TResponse>` | Transient | Pipeline behaviors (manually registered) |

## Examples

See the [`/docs`](docs/) folder for complete examples:

- [Command](docs/command.md)
- [Command with result](docs/command-with-result.md)
- [Query](docs/query.md)
- [Notification](docs/notification.md)
- [Multiple notification handlers](docs/multiple-handlers.md)
- [Pipeline behavior](docs/pipeline-behavior.md)
- [DI registration](docs/di-registration.md)
- [Assembly scanning](docs/assembly-scanning.md)
- [Cancellation](docs/cancellation.md)
- [Result-based responses](docs/result-responses.md)
- [Testing](docs/testing.md)

## Testing

CommandFlow is designed to be easy to test. You can:

1. **Test handlers directly** — they're simple classes with a `Handle` method
2. **Mock `IMediator`** — verify that the correct requests are sent
3. **Integration test with real DI** — build a real service provider for end-to-end tests

```csharp
// Direct handler test
var handler = new GetUserQueryHandler();
var result = await handler.Handle(new GetUserQuery(1), CancellationToken.None);
Assert.Equal("Alice", result.Name);

// Mock mediator
var mediator = new Mock<IMediator>();
mediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new UserDto(1, "Alice"));
```

## Packaging

The library is split into two NuGet packages:

- **`CommandFlow`** — core abstractions and mediator implementation (zero external dependencies)
- **`CommandFlow.DependencyInjection`** — DI registration extensions (depends on `Microsoft.Extensions.DependencyInjection.Abstractions`)

## Benchmarks

Run benchmarks with:

```bash
dotnet run --project benchmarks/CommandFlow.Benchmarks -c Release -- --filter "*"
```

Benchmarks measure:
- Request dispatch overhead
- Pipeline behavior overhead
- Notification publishing with multiple handlers

## Architecture

```
IMediator : ISender, IPublisher
    │
    ├── Send<TResponse>(IRequest<TResponse>)
    │       │
    │       ├── IPipelineBehavior<TRequest, TResponse> (0..N)
    │       │       │
    │       │       └── IRequestHandler<TRequest, TResponse>
    │       │
    │       ├── ICommand → IRequest<Unit>
    │       ├── ICommand<TResult> → IRequest<TResult>
    │       └── IQuery<TResult> → IRequest<TResult>
    │
    └── Publish<TNotification>(TNotification)
            │
            └── INotificationHandler<TNotification> (0..N, sequential)
```

## License

[MIT](LICENSE)

