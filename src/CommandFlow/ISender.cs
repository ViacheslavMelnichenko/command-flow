namespace CommandFlow;

/// <summary>
/// Sends a request through the pipeline to be handled by a single handler.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request to a single handler.
    /// </summary>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the send operation, containing the handler response.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

