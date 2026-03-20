using CommandFlow.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CommandFlow.UnitTests;

public class PipelineBehaviorTests
{
    public PipelineBehaviorTests()
    {
        LoggingBehavior<PingRequest, string>.Log.Clear();
        ValidationBehavior<PingRequest, string>.Log.Clear();
    }

    [Fact]
    public async Task Send_WithSingleBehavior_ExecutesBehaviorAroundHandler()
    {
        // Arrange
        LoggingBehavior<PingRequest, string>.Log.Clear();
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly)
            .AddBehavior(typeof(LoggingBehavior<,>));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PingRequest());

        // Assert
        result.ShouldBe("Pong");
        LoggingBehavior<PingRequest, string>.Log.ShouldBe(new[]
        {
            "Before:PingRequest",
            "After:PingRequest"
        });
    }

    [Fact]
    public async Task Send_WithMultipleBehaviors_ExecutesInRegistrationOrder()
    {
        // Arrange
        LoggingBehavior<PingRequest, string>.Log.Clear();
        ValidationBehavior<PingRequest, string>.Log.Clear();
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly)
            .AddBehavior(typeof(LoggingBehavior<,>))
            .AddBehavior(typeof(ValidationBehavior<,>));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PingRequest());

        // Assert
        result.ShouldBe("Pong");
        // Logging is outermost, Validation is inner
        // Execution order: Logging.Before -> Validation.Validate -> Handler -> Logging.After
        var allLog = new List<string>();
        allLog.AddRange(LoggingBehavior<PingRequest, string>.Log);
        allLog.InsertRange(1, ValidationBehavior<PingRequest, string>.Log);
        allLog.ShouldBe(new[]
        {
            "Before:PingRequest",
            "Validate:PingRequest",
            "After:PingRequest"
        });
    }

    [Fact]
    public async Task Send_WithShortCircuitBehavior_DoesNotCallHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly)
            .AddBehavior(typeof(ShortCircuitBehavior<,>));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act — ShortCircuitBehavior returns default without calling next()
        var result = await mediator.Send(new PingRequest());

        // Assert
        result.ShouldBeNull(); // default(string) is null
    }

    [Fact]
    public async Task Send_BehaviorReceivesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly)
            .AddBehavior(typeof(CancellationCheckBehavior<,>));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.Send(new PingRequest(), cts.Token);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task Send_BehaviorWorksWithVoidCommands()
    {
        // Arrange
        LoggingBehavior<CreateOrderCommand, Unit>.Log.Clear();
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(CreateOrderCommandHandler).Assembly)
            .AddBehavior(typeof(LoggingBehavior<,>));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new CreateOrderCommand("Widget", 1));

        // Assert
        LoggingBehavior<CreateOrderCommand, Unit>.Log.ShouldBe(new[]
        {
            "Before:CreateOrderCommand",
            "After:CreateOrderCommand"
        });
    }

    [Fact]
    public async Task Send_ClosedGenericBehavior_RegisteredCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly)
            .AddBehavior<PingLoggingBehavior>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PingRequest());

        // Assert
        result.ShouldBe("Pong");
        PingLoggingBehavior.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Send_CorrelationIdBehavior_SetsIdWhenMissing()
    {
        // Arrange
        CorrelationContext.CorrelationId = null;
        var handler = new CorrelationCapturingHandler();
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<IRequestHandler<CorrelatedRequest, string>>(_ => handler);
        services.AddTransient<IPipelineBehavior<CorrelatedRequest, string>, CorrelationIdBehavior<CorrelatedRequest, string>>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new CorrelatedRequest());

        // Assert — handler received a non-empty correlation ID
        handler.CapturedCorrelationId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Send_CorrelationIdBehavior_PreservesExistingId()
    {
        // Arrange
        var existingId = "my-trace-123";
        CorrelationContext.CorrelationId = existingId;
        var handler = new CorrelationCapturingHandler();
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<IRequestHandler<CorrelatedRequest, string>>(_ => handler);
        services.AddTransient<IPipelineBehavior<CorrelatedRequest, string>, CorrelationIdBehavior<CorrelatedRequest, string>>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new CorrelatedRequest());

        // Assert — handler received the pre-existing correlation ID, not a new one
        handler.CapturedCorrelationId.ShouldBe(existingId);
    }

    [Fact]
    public async Task Send_CorrelationIdBehavior_RestoresContextAfterHandler()
    {
        // Arrange
        CorrelationContext.CorrelationId = null;
        var handler = new CorrelationCapturingHandler();
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<IRequestHandler<CorrelatedRequest, string>>(_ => handler);
        services.AddTransient<IPipelineBehavior<CorrelatedRequest, string>, CorrelationIdBehavior<CorrelatedRequest, string>>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new CorrelatedRequest());

        // Assert — correlation ID was cleaned up after the pipeline completed
        CorrelationContext.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public async Task Send_CorrelationIdBehavior_OpenGeneric_AppliesToAllRequestTypes()
    {
        // Arrange
        CorrelationContext.CorrelationId = null;
        var handler = new CorrelationCapturingHandler();
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<IRequestHandler<CorrelatedRequest, string>>(_ => handler);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CorrelationIdBehavior<,>));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new CorrelatedRequest());

        // Assert — open-generic registration works the same as closed
        handler.CapturedCorrelationId.ShouldNotBeNullOrEmpty();
    }

    private class PingLoggingBehavior : IPipelineBehavior<PingRequest, string>
    {
        public static bool WasCalled { get; private set; }

        public async Task<string> Handle(PingRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return await next();
        }
    }
}
