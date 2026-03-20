# Pipeline Behavior

Pipeline behaviors are middleware that wrap request handling. They run for every request that matches their type constraints.

## Define a behavior

```csharp
using CommandFlow;

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
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {Request}", requestName);

        var response = await next();

        _logger.LogInformation("Handled {Request}", requestName);
        return response;
    }
}
```

## Register the behavior

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddBehavior(typeof(LoggingBehavior<,>));
```

## Execution order

Behaviors wrap the handler in registration order. The first registered behavior is the **outermost** wrapper:

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddBehavior(typeof(LoggingBehavior<,>))       // 1st: outermost
    .AddBehavior(typeof(ValidationBehavior<,>))     // 2nd: inner
    .AddBehavior(typeof(PerformanceBehavior<,>));   // 3rd: closest to handler
```

Execution flow:

```
→ LoggingBehavior.Before
  → ValidationBehavior.Before
    → PerformanceBehavior.Before
      → Handler
    ← PerformanceBehavior.After
  ← ValidationBehavior.After
← LoggingBehavior.After
```

## Short-circuiting

A behavior can skip calling `next()` to short-circuit the pipeline. The handler and inner behaviors will not execute.

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Validate...
        if (!IsValid(request))
            throw new ValidationException("Request is invalid");

        return next();
    }
}
```

## Closed generic behaviors

You can also register behaviors that apply to a specific request type:

```csharp
public class CreateUserValidation : IPipelineBehavior<CreateUserCommand, int>
{
    public async Task<int> Handle(
        CreateUserCommand request,
        RequestHandlerDelegate<int> next,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Name))
            throw new ArgumentException("Name is required");

        return await next();
    }
}

// Register with generic syntax
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddBehavior<CreateUserValidation>();
```

## Notes

- Pipeline behaviors only apply to requests (commands and queries), not notifications
- Behaviors are resolved from the DI container — they support constructor injection
- The `RequestHandlerDelegate<TResponse>` delegate represents the next step in the pipeline

## Cross-cutting concerns

Open-generic behaviors apply to **every** request, making them ideal for cross-cutting concerns like correlation ID propagation, logging, metrics, or authorization. Register them once and they wrap all commands and queries automatically.

### Correlation ID example

```csharp
using CommandFlow;

public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public static string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }
}

public class CorrelationIdBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var existingId = CorrelationContext.CorrelationId;
        if (string.IsNullOrEmpty(existingId))
        {
            CorrelationContext.CorrelationId = Guid.NewGuid().ToString();
        }

        try
        {
            return await next();
        }
        finally
        {
            CorrelationContext.CorrelationId = existingId;
        }
    }
}
```

Register it as the outermost behavior so every handler sees the correlation ID:

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddBehavior(typeof(CorrelationIdBehavior<,>))   // outermost — sets correlation ID
    .AddBehavior(typeof(LoggingBehavior<,>))          // can read CorrelationContext.CorrelationId
    .AddBehavior(typeof(ValidationBehavior<,>));
```

Handlers and inner behaviors access the ID via `CorrelationContext.CorrelationId`. The `finally` block ensures the ambient context is restored after each request completes.

### Other common cross-cutting behaviors

| Concern | Pattern |
|---------|---------|
| **Logging** | Log request type + elapsed time around `next()` |
| **Validation** | Throw before `next()` if request is invalid |
| **Authorization** | Check permissions, short-circuit if denied |
| **Performance** | `Stopwatch` around `next()`, log slow requests |
| **Exception handling** | Catch exceptions from `next()`, transform or log |
| **Retry** | Call `next()` in a retry loop |

