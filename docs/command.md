# Command (void)

A command that performs an action without returning a result.

## Define the command

```csharp
using CommandFlow;

public record DeleteUserCommand(int UserId) : ICommand;
```

## Define the handler

Implement `ICommandHandler<TCommand>` for void commands. Use `HandleCommand` instead of `Handle`.

```csharp
public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    private readonly IUserRepository _users;

    public DeleteUserCommandHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task HandleCommand(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        await _users.DeleteAsync(command.UserId, cancellationToken);
    }
}
```

## Send the command

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Send(new DeleteUserCommand(42));
```

## Notes

- `ICommand` extends `IRequest<Unit>` internally
- `ICommandHandler<T>` provides a default `Handle` implementation that calls `HandleCommand` and returns `Unit.Value`
- You get back a `Unit` result which can be discarded

