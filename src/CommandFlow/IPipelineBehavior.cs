namespace CommandFlow;

/// <summary>
/// Defines a pipeline behavior that wraps request handling.
/// Behaviors are executed in the order they are registered.
/// The first registered behavior is the outermost wrapper.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Pipeline handler. Perform any additional behavior and call <paramref name="next"/>
    /// to continue the pipeline.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The delegate for the next action in the pipeline.
    /// Call this to continue the pipeline or skip it to short-circuit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>
/// Represents an async continuation for the next task to execute in the pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of response returned.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

