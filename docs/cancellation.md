# Cancellation

`CancellationToken` flows through the entire CommandFlow pipeline: from the caller, through all pipeline behaviors, to the handler.

## Passing a CancellationToken

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var result = await mediator.Send(new GetUserQuery(1), cts.Token);
```

## Using CancellationToken in handlers

```csharp
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IDbConnection _db;

    public GetUserQueryHandler(IDbConnection db) => _db = db;

    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Pass the token to async operations
        var user = await _db.QuerySingleAsync<UserDto>(
            "SELECT * FROM Users WHERE Id = @Id",
            new { request.UserId },
            cancellationToken: cancellationToken);

        return user;
    }
}
```

## Using CancellationToken in behaviors

```csharp
public class TimeoutBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check before proceeding
        cancellationToken.ThrowIfCancellationRequested();

        return await next();
    }
}
```

## Using CancellationToken in notification handlers

```csharp
public class AuditLogHandler : INotificationHandler<OrderPlacedEvent>
{
    public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await SaveAuditLog(notification, cancellationToken);
    }
}
```

## Default behavior

If no `CancellationToken` is provided, `CancellationToken.None` (default) is used. This means no cancellation will be triggered.

```csharp
// These are equivalent:
await mediator.Send(new GetUserQuery(1));
await mediator.Send(new GetUserQuery(1), CancellationToken.None);
```

## Notes

- The token flows through the pipeline unchanged — behaviors cannot replace it
- `OperationCanceledException` propagates normally through the pipeline
- Notification handlers also receive the token from the `Publish` call

