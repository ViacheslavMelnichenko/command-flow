# Query

A query that retrieves data and returns a result.

## Define the query

```csharp
using CommandFlow;

public record GetUserByIdQuery(int UserId) : IQuery<UserDto>;

public record UserDto(int Id, string Name, string Email);
```

## Define the handler

```csharp
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _users;

    public GetUserByIdQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        return new UserDto(user.Id, user.Name, user.Email);
    }
}
```

## Send the query

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();
var user = await mediator.Send(new GetUserByIdQuery(1));
Console.WriteLine($"User: {user.Name}");
```

## Notes

- `IQuery<TResult>` extends `IRequest<TResult>`
- Queries always return a value
- Queries and commands with results use the same handler interface (`IRequestHandler<TRequest, TResponse>`)
- The `IQuery<T>` vs `ICommand<T>` distinction communicates intent: queries read, commands write

