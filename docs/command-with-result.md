# Command with Result

A command that performs an action and returns a result.

## Define the command

```csharp
using CommandFlow;

public record CreateUserCommand(string Name, string Email) : ICommand<int>;
```

## Define the handler

For commands with a result, implement `IRequestHandler<TRequest, TResponse>` directly.

```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    private readonly IUserRepository _users;

    public CreateUserCommandHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User(request.Name, request.Email);
        await _users.AddAsync(user, cancellationToken);
        return user.Id;
    }
}
```

## Send the command

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();
int userId = await mediator.Send(new CreateUserCommand("Alice", "alice@example.com"));
Console.WriteLine($"Created user with ID: {userId}");
```

## Notes

- `ICommand<TResult>` extends `IRequest<TResult>`
- The handler uses `IRequestHandler<TRequest, TResponse>` (same as queries)
- The semantic distinction between `ICommand<T>` and `IQuery<T>` is for developer intent — both use the same dispatch mechanism

