using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace CommandFlow;

/// <summary>
/// Default mediator implementation that dispatches requests through the pipeline
/// and publishes notifications to all registered handlers.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a new <see cref="Mediator"/> instance.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers and behaviors.</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler registered for {requestType.Name}. " +
                $"Ensure an IRequestHandler<{requestType.Name}, {typeof(TResponse).Name}> is registered in the container.");

        // Resolve pipeline behaviors
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = (IEnumerable<object>?)_serviceProvider.GetService(
            typeof(IEnumerable<>).MakeGenericType(behaviorType));

        // Build the pipeline: behaviors wrap the handler call
        RequestHandlerDelegate<TResponse> pipeline = () =>
            InvokeAsync<TResponse>(handlerType.GetMethod("Handle")!, handler, [request, cancellationToken]);

        if (behaviors is not null)
        {
            // Reverse so that first-registered behavior is outermost
            foreach (var behavior in behaviors.Reverse())
            {
                var current = pipeline;
                var handleMethod = behaviorType.GetMethod("Handle")!;
                pipeline = () => InvokeAsync<TResponse>(handleMethod, behavior, [request, current, cancellationToken]);
            }
        }

        return pipeline();
    }

    /// <inheritdoc />
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
        var handlers = (IEnumerable<object>?)_serviceProvider.GetService(
            typeof(IEnumerable<>).MakeGenericType(handlerType));

        if (handlers is null)
            return;

        // Sequential execution: handlers run one at a time in registration order.
        // This is the simplest and most predictable strategy.
        // If a handler throws, subsequent handlers will not execute.
        var handleMethod = handlerType.GetMethod("Handle")!;
        foreach (var handler in handlers)
        {
            try
            {
                await ((Task)handleMethod.Invoke(handler, [notification, cancellationToken])!).ConfigureAwait(false);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw Rethrow(ex.InnerException);
            }
        }
    }

    /// <summary>
    /// Invokes a method via reflection and unwraps TargetInvocationException.
    /// </summary>
    private static async Task<TResult> InvokeAsync<TResult>(MethodInfo method, object target, object?[] args)
    {
        try
        {
            return await (Task<TResult>)method.Invoke(target, args)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw Rethrow(ex.InnerException);
        }
    }

    [DoesNotReturn]
    [ExcludeFromCodeCoverage]
    private static Exception Rethrow(Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
        return exception; // unreachable, but required by compiler
    }
}
