# Result-Based Responses

CommandFlow is fully compatible with [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions) Result types. No special integration package is needed.

## Why it just works

CommandFlow's generic pipeline treats `TResponse` as an opaque type:

```
ICommand<TResult> : IRequest<TResult>
IQuery<TResult> : IRequest<TResult>
IRequestHandler<TRequest, TResponse>
IPipelineBehavior<TRequest, TResponse>
```

When `TResult` is `Result`, `Result<T>`, `UnitResult<TError>`, or `Result<TValue, TError>`, everything works the same way — dispatching, handler invocation, pipeline behaviors, and DI resolution are all unaffected.

There is no special layer, no wrapper, and no integration package. Result types are ordinary generic response types.

## When to use Result vs exceptions

| Scenario | Approach |
|---|---|
| Domain validation ("quantity must be positive") | `Result.Failure(...)` |
| Business rule violation ("insufficient funds") | `Result.Failure(...)` |
| Entity not found | `Result.Failure(...)` |
| Unexpected infrastructure error (database down, network timeout) | Let the exception throw |
| Cancellation | `OperationCanceledException` (standard .NET) |
| Null arguments, programming errors | `ArgumentNullException`, `InvalidOperationException` |

**Rule of thumb**: if the caller is expected to handle the failure as part of normal control flow, return a `Result`. If the failure is unexpected and represents a bug or infrastructure issue, throw an exception.

A `Result.Failure(...)` is a **value**, not an exception. It flows through the pipeline normally. Behaviors see it as a successful return — only the caller inspects `IsSuccess` / `IsFailure`.

## Command returning Result

For a command that succeeds or fails with no return value:

```csharp
using CommandFlow;
using CSharpFunctionalExtensions;

public record PlaceOrderCommand(string ProductName, int Quantity) : ICommand<Result>;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Result>
{
    public Task<Result> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            return Task.FromResult(Result.Failure("Quantity must be positive"));

        // ... place the order
        return Task.FromResult(Result.Success());
    }
}
```

Usage:

```csharp
var result = await mediator.Send(new PlaceOrderCommand("Widget", 3));
if (result.IsFailure)
{
    Console.WriteLine(result.Error);
    return;
}
```

## Command returning Result&lt;T&gt;

For a command that returns a value on success:

```csharp
public record CreateOrderCommand(string ProductName, int Quantity) : ICommand<Result<int>>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<int>>
{
    public Task<Result<int>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            return Task.FromResult(Result.Failure<int>("Quantity must be positive"));

        return Task.FromResult(Result.Success(42));
    }
}
```

## Query returning Result&lt;T&gt;

```csharp
public record FindOrderQuery(int OrderId) : IQuery<Result<OrderDto>>;

public class FindOrderHandler : IRequestHandler<FindOrderQuery, Result<OrderDto>>
{
    public Task<Result<OrderDto>> Handle(FindOrderQuery request, CancellationToken cancellationToken)
    {
        if (request.OrderId <= 0)
            return Task.FromResult(Result.Failure<OrderDto>("Invalid order ID"));

        var order = new OrderDto(request.OrderId, "Widget", 5);
        return Task.FromResult(Result.Success(order));
    }
}
```

## UnitResult&lt;TError&gt;

For void commands with a typed error:

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

## Result&lt;TValue, TError&gt;

For rich typed errors:

```csharp
public record TransferError(string Code, string Message);

public record TransferFundsCommand(decimal Amount, string From, string To)
    : ICommand<Result<decimal, TransferError>>;

public class TransferFundsHandler
    : IRequestHandler<TransferFundsCommand, Result<decimal, TransferError>>
{
    public Task<Result<decimal, TransferError>> Handle(
        TransferFundsCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return Task.FromResult(
                Result.Failure<decimal, TransferError>(
                    new TransferError("INVALID_AMOUNT", "Amount must be positive")));

        return Task.FromResult(Result.Success<decimal, TransferError>(request.Amount));
    }
}
```

## Query returning Result&lt;TValue, TError&gt;

```csharp
public record FindOrderStrictQuery(int OrderId) : IQuery<Result<OrderDto, string>>;

public class FindOrderStrictHandler
    : IRequestHandler<FindOrderStrictQuery, Result<OrderDto, string>>
{
    public Task<Result<OrderDto, string>> Handle(
        FindOrderStrictQuery request, CancellationToken cancellationToken)
    {
        if (request.OrderId <= 0)
            return Task.FromResult(Result.Failure<OrderDto, string>("Order not found"));

        return Task.FromResult(
            Result.Success<OrderDto, string>(new OrderDto(request.OrderId, "Widget", 5)));
    }
}
```

## Pipeline behaviors

Generic behaviors work transparently. A `Result.Failure(...)` flows through the behavior chain as a normal response value:

```csharp
services.AddCommandFlow(typeof(PlaceOrderHandler).Assembly)
    .AddBehavior(typeof(LoggingBehavior<,>))
    .AddBehavior(typeof(ValidationBehavior<,>));
```

If you want a behavior that inspects the Result:

```csharp
public class ResultLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (response is Result result && result.IsFailure)
            Console.WriteLine($"Request {typeof(TRequest).Name} failed: {result.Error}");

        return response;
    }
}
```

## DI registration

Result-based handlers are discovered by assembly scanning automatically — they implement `IRequestHandler<TRequest, TResponse>` like any other handler:

```csharp
services.AddCommandFlow(typeof(PlaceOrderHandler).Assembly);
```

No additional registration is needed.

## Notifications

Notifications remain exception-based. `INotification` / `INotificationHandler<T>` do not return values, so Result types do not apply. If a notification handler encounters a domain failure it wants to communicate, it should use other mechanisms (e.g., publish another notification, log, or throw if truly exceptional).

## Notes

- CommandFlow has **no dependency** on CSharpFunctionalExtensions — the core library remains dependency-free
- CSharpFunctionalExtensions is a dependency of **your application**, not of CommandFlow
- Result types are ordinary `TResponse` values — no special wiring needed
- Behaviors see `Result.Failure(...)` as a successful handler return, not an exception
- Unexpected infrastructure exceptions still propagate normally through the pipeline

