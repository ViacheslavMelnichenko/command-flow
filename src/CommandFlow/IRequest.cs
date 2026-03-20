namespace CommandFlow;

/// <summary>
/// Marker interface for requests that return a response.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
public interface IRequest<out TResponse>;

/// <summary>
/// Marker interface for commands that do not return a value.
/// Equivalent to <see cref="IRequest{TResponse}"/> with <see cref="Unit"/>.
/// </summary>
public interface ICommand : IRequest<Unit>;

/// <summary>
/// Marker interface for commands that return a result.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command handler.</typeparam>
public interface ICommand<out TResult> : IRequest<TResult>;

/// <summary>
/// Marker interface for queries that return a result.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query handler.</typeparam>
public interface IQuery<out TResult> : IRequest<TResult>;

