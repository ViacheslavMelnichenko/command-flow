using CommandFlow.UnitTests.Fakes;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CommandFlow.UnitTests;

/// <summary>
/// Verifies that CSharpFunctionalExtensions Result types work as ordinary TResponse values
/// through the full CommandFlow pipeline — no special integration required.
/// </summary>
public class ResultCompatibilityTests
{
    // --- ICommand<Result> ---

    [Fact]
    public async Task Send_CommandReturningResult_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PlaceOrderCommandHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PlaceOrderCommand("Widget", 3));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Send_CommandReturningResult_Failure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PlaceOrderCommandHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PlaceOrderCommand("Widget", -1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Quantity must be positive");
    }

    // --- ICommand<Result<T>> ---

    [Fact]
    public async Task Send_CommandReturningResultT_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PlaceOrderWithIdCommandHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PlaceOrderWithIdCommand("Widget", 3));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public async Task Send_CommandReturningResultT_Failure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PlaceOrderWithIdCommandHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PlaceOrderWithIdCommand("Widget", 0));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Quantity must be positive");
    }

    // --- ICommand<Result<TValue, TError>> ---

    [Fact]
    public async Task Send_CommandReturningResultTValueTError_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(TransferFundsCommandHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new TransferFundsCommand(100m, "A", "B"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(100m);
    }

    [Fact]
    public async Task Send_CommandReturningResultTValueTError_Failure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(TransferFundsCommandHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new TransferFundsCommand(-50m, "A", "B"));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INVALID_AMOUNT");
        result.Error.Message.ShouldBe("Amount must be positive");
    }

    // --- ICommand<UnitResult<TError>> ---

    [Fact]
    public async Task Send_CommandReturningUnitResult_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(ArchiveOrderCommandHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new ArchiveOrderCommand(1));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Send_CommandReturningUnitResult_Failure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(ArchiveOrderCommandHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new ArchiveOrderCommand(-1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Invalid order ID");
    }

    // --- IQuery<Result<T>> ---

    [Fact]
    public async Task Send_QueryReturningResultT_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(FindOrderQueryHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new FindOrderQuery(7));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(7);
        result.Value.ProductName.ShouldBe("Widget");
    }

    [Fact]
    public async Task Send_QueryReturningResultT_Failure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(FindOrderQueryHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new FindOrderQuery(-1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Invalid order ID");
    }

    // --- IQuery<Result<TValue, TError>> ---

    [Fact]
    public async Task Send_QueryReturningResultTValueTError_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(FindOrderStrictQueryHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new FindOrderStrictQuery(7));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(7);
    }

    [Fact]
    public async Task Send_QueryReturningResultTValueTError_Failure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(FindOrderStrictQueryHandler).Assembly);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new FindOrderStrictQuery(-1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Order not found");
    }

    // --- Pipeline behavior compatibility ---

    [Fact]
    public async Task Send_ResultCommand_WithPipelineBehavior_ResponsePassesThrough()
    {
        // Arrange
        LoggingBehavior<PlaceOrderCommand, Result>.Log.Clear();
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PlaceOrderCommandHandler).Assembly)
            .AddBehavior(typeof(LoggingBehavior<,>));
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PlaceOrderCommand("Widget", 3));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        LoggingBehavior<PlaceOrderCommand, Result>.Log.ShouldBe(new[]
        {
            "Before:PlaceOrderCommand",
            "After:PlaceOrderCommand"
        });
    }

    [Fact]
    public async Task Send_ResultQuery_FailurePassesThroughBehaviorWithoutException()
    {
        // Arrange
        LoggingBehavior<FindOrderQuery, Result<OrderDto>>.Log.Clear();
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(FindOrderQueryHandler).Assembly)
            .AddBehavior(typeof(LoggingBehavior<,>));
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new FindOrderQuery(-1));

        // Assert — failure is a value, not an exception — it passes through the behavior normally
        result.IsFailure.ShouldBeTrue();
        LoggingBehavior<FindOrderQuery, Result<OrderDto>>.Log.ShouldBe(new[]
        {
            "Before:FindOrderQuery",
            "After:FindOrderQuery"
        });
    }

    // --- Unexpected exceptions still propagate ---

    [Fact]
    public async Task Send_ResultHandler_UnexpectedExceptionStillThrows()
    {
        // Arrange — manual registration only, no assembly scanning
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<IRequestHandler<PlaceOrderCommand, Result>>(_ =>
            new DelegateRequestHandler<PlaceOrderCommand, Result>((_, _) =>
                throw new InvalidOperationException("Unexpected infrastructure error")));
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.Send(new PlaceOrderCommand("Widget", 1));

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(act);
    }

    /// <summary>
    /// Generic delegate-based handler. Because it's an open generic, assembly scanning
    /// excludes it (IsGenericTypeDefinition = true).
    /// </summary>
    private class DelegateRequestHandler<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<TResponse>> handler)
        : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}

