using CommandFlow;
using CSharpFunctionalExtensions;

namespace CommandFlow.UnitTests.Fakes;

// --- Types for branch coverage of assembly scanning filters ---
// These exist so the assembly contains abstract, interface, and non-generic-interface types,
// exercising all branches of the pattern match: t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }

public abstract class AbstractHandler;

public interface IMarkerInterface;

public class NonGenericInterfaceType : IMarkerInterface;

// --- Commands ---

public record CreateOrderCommand(string ProductName, int Quantity) : ICommand;

public record CreateOrderWithResultCommand(string ProductName, int Quantity) : ICommand<int>;

// --- Queries ---

public record GetOrderByIdQuery(int OrderId) : IQuery<OrderDto>;

public record OrderDto(int Id, string ProductName, int Quantity);

// --- Notifications ---

public record OrderCreatedNotification(int OrderId, string ProductName) : INotification;

// --- Request (generic) ---

public record PingRequest : IRequest<string>;

// --- Result-based Commands & Queries (Result is just TResponse) ---

public record PlaceOrderCommand(string ProductName, int Quantity) : ICommand<Result>;

public record PlaceOrderWithIdCommand(string ProductName, int Quantity) : ICommand<Result<int>>;

public record TransferFundsCommand(decimal Amount, string From, string To)
    : ICommand<Result<decimal, TransferError>>;

public record TransferError(string Code, string Message);

public record ArchiveOrderCommand(int OrderId) : ICommand<UnitResult<string>>;

public record FindOrderQuery(int OrderId) : IQuery<Result<OrderDto>>;

public record FindOrderStrictQuery(int OrderId) : IQuery<Result<OrderDto, string>>;

// --- Handlers ---

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand>
{
    public bool WasCalled { get; private set; }
    public CancellationToken ReceivedToken { get; private set; }

    public Task HandleCommand(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        WasCalled = true;
        ReceivedToken = cancellationToken;
        return Task.CompletedTask;
    }
}

public class CreateOrderWithResultCommandHandler : IRequestHandler<CreateOrderWithResultCommand, int>
{
    public bool WasCalled { get; private set; }

    public Task<int> Handle(CreateOrderWithResultCommand request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Task.FromResult(42);
    }
}

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public bool WasCalled { get; private set; }

    public Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Task.FromResult(new OrderDto(request.OrderId, "Widget", 5));
    }
}

public class PingRequestHandler : IRequestHandler<PingRequest, string>
{
    public Task<string> Handle(PingRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Pong");
    }
}

// --- Notification Handlers ---

public class OrderCreatedEmailHandler : INotificationHandler<OrderCreatedNotification>
{
    public bool WasCalled { get; private set; }
    public int CallOrder { get; private set; }
    private static int _callCounter;

    public static void ResetCounter() => _callCounter = 0;

    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        WasCalled = true;
        CallOrder = Interlocked.Increment(ref _callCounter);
        return Task.CompletedTask;
    }
}

public class OrderCreatedLoggingHandler : INotificationHandler<OrderCreatedNotification>
{
    public bool WasCalled { get; private set; }
    public int CallOrder { get; private set; }
    private static int _callCounter;

    public static void ResetCounter() => _callCounter = 0;

    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        WasCalled = true;
        CallOrder = Interlocked.Increment(ref _callCounter);
        return Task.CompletedTask;
    }
}

public class ThrowingNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler failed");
    }
}

// --- Pipeline Behaviors ---

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static readonly List<string> Log = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Log.Add($"Before:{typeof(TRequest).Name}");
        var response = await next();
        Log.Add($"After:{typeof(TRequest).Name}");
        return response;
    }
}

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static readonly List<string> Log = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Log.Add($"Validate:{typeof(TRequest).Name}");
        return await next();
    }
}

public class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Does not call next() — short-circuits the pipeline
        return Task.FromResult(default(TResponse)!);
    }
}

public class CancellationCheckBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await next();
    }
}

// --- Cross-cutting concern: Correlation ID propagation ---

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
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
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

public record CorrelatedRequest : IRequest<string>;

public class CorrelationCapturingHandler : IRequestHandler<CorrelatedRequest, string>
{
    public string? CapturedCorrelationId { get; private set; }

    public Task<string> Handle(CorrelatedRequest request, CancellationToken cancellationToken)
    {
        CapturedCorrelationId = CorrelationContext.CorrelationId;
        return Task.FromResult("Pong");
    }
}

// --- Throwing Handler (synchronous throw to trigger TargetInvocationException) ---

public record ThrowingRequest : IRequest<string>;

public class ThrowingRequestHandler : IRequestHandler<ThrowingRequest, string>
{
    public Task<string> Handle(ThrowingRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler exploded");
    }
}

// --- Closed-generic Pipeline Behavior for Type-overload tests ---

public class ClosedPingValidationBehavior : IPipelineBehavior<PingRequest, string>
{
    public static bool WasCalled { get; set; }

    public async Task<string> Handle(PingRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return await next();
    }
}

// --- Result-based Handlers (Result types are ordinary TResponse values) ---

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Result>
{
    public Task<Result> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            return Task.FromResult(Result.Failure("Quantity must be positive"));

        return Task.FromResult(Result.Success());
    }
}

public class PlaceOrderWithIdCommandHandler : IRequestHandler<PlaceOrderWithIdCommand, Result<int>>
{
    public Task<Result<int>> Handle(PlaceOrderWithIdCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            return Task.FromResult(Result.Failure<int>("Quantity must be positive"));

        return Task.FromResult(Result.Success(42));
    }
}

public class TransferFundsCommandHandler : IRequestHandler<TransferFundsCommand, Result<decimal, TransferError>>
{
    public Task<Result<decimal, TransferError>> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return Task.FromResult(Result.Failure<decimal, TransferError>(
                new TransferError("INVALID_AMOUNT", "Amount must be positive")));

        return Task.FromResult(Result.Success<decimal, TransferError>(request.Amount));
    }
}

public class ArchiveOrderCommandHandler : IRequestHandler<ArchiveOrderCommand, UnitResult<string>>
{
    public Task<UnitResult<string>> Handle(ArchiveOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.OrderId <= 0)
            return Task.FromResult(UnitResult.Failure("Invalid order ID"));

        return Task.FromResult(UnitResult.Success<string>());
    }
}

public class FindOrderQueryHandler : IRequestHandler<FindOrderQuery, Result<OrderDto>>
{
    public Task<Result<OrderDto>> Handle(FindOrderQuery request, CancellationToken cancellationToken)
    {
        if (request.OrderId <= 0)
            return Task.FromResult(Result.Failure<OrderDto>("Invalid order ID"));

        return Task.FromResult(Result.Success(new OrderDto(request.OrderId, "Widget", 5)));
    }
}

public class FindOrderStrictQueryHandler : IRequestHandler<FindOrderStrictQuery, Result<OrderDto, string>>
{
    public Task<Result<OrderDto, string>> Handle(FindOrderStrictQuery request, CancellationToken cancellationToken)
    {
        if (request.OrderId <= 0)
            return Task.FromResult(Result.Failure<OrderDto, string>("Order not found"));

        return Task.FromResult(Result.Success<OrderDto, string>(new OrderDto(request.OrderId, "Widget", 5)));
    }
}

