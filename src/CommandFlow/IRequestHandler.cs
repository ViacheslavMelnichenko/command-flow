namespace CommandFlow;

/// <summary>
/// Defines a handler for a request that returns a response.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles a request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response from the handler.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for a command that does not return a value.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Unit>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles a command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleCommand(TCommand command, CancellationToken cancellationToken);

    async Task<Unit> IRequestHandler<TCommand, Unit>.Handle(TCommand request, CancellationToken cancellationToken)
    {
        await HandleCommand(request, cancellationToken);
        return Unit.Value;
    }
}

