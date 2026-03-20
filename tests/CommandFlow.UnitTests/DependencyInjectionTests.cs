using CommandFlow.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CommandFlow.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddCommandFlow_RegistersMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var mediator = provider.GetService<IMediator>();

        // Assert
        mediator.ShouldNotBeNull();
    }

    [Fact]
    public void AddCommandFlow_RegistersISender()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var sender = provider.GetService<ISender>();

        // Assert
        sender.ShouldNotBeNull();
    }

    [Fact]
    public void AddCommandFlow_RegistersIPublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var publisher = provider.GetService<IPublisher>();

        // Assert
        publisher.ShouldNotBeNull();
    }

    [Fact]
    public void AddCommandFlow_ISenderResolvesToMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var sender = provider.GetRequiredService<ISender>();

        // Assert
        sender.ShouldBeOfType<Mediator>();
    }

    [Fact]
    public void AddCommandFlow_ScansAndRegistersRequestHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var handler = provider.GetService<IRequestHandler<PingRequest, string>>();

        // Assert
        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<PingRequestHandler>();
    }

    [Fact]
    public void AddCommandFlow_ScansAndRegistersCommandHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(CreateOrderCommandHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var handler = provider.GetService<IRequestHandler<CreateOrderCommand, Unit>>();

        // Assert
        handler.ShouldNotBeNull();
    }

    [Fact]
    public void AddCommandFlow_ScansAndRegistersNotificationHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(OrderCreatedEmailHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var handlers = provider.GetServices<INotificationHandler<OrderCreatedNotification>>().ToList();

        // Assert
        handlers.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void AddCommandFlow_WithNoAssemblies_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() => services.AddCommandFlow());
    }

    [Fact]
    public void AddBehavior_OpenGeneric_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly)
            .AddBehavior(typeof(LoggingBehavior<,>));
        var provider = services.BuildServiceProvider();

        // Act
        var behaviors = provider.GetServices<IPipelineBehavior<PingRequest, string>>().ToList();

        // Assert
        behaviors.Count.ShouldBe(1);
        behaviors[0].ShouldBeOfType<LoggingBehavior<PingRequest, string>>();
    }

    [Fact]
    public void AddBehavior_MultipleBehaviors_AllRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly)
            .AddBehavior(typeof(LoggingBehavior<,>))
            .AddBehavior(typeof(ValidationBehavior<,>));
        var provider = services.BuildServiceProvider();

        // Act
        var behaviors = provider.GetServices<IPipelineBehavior<PingRequest, string>>().ToList();

        // Assert
        behaviors.Count.ShouldBe(2);
    }

    [Fact]
    public void AddBehavior_InvalidType_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = services.AddCommandFlow(typeof(PingRequestHandler).Assembly);

        // Act & Assert
        Should.Throw<ArgumentException>(() => config.AddBehavior(typeof(string)));
    }

    [Fact]
    public void AddHandlersFromAssemblies_RegistersAdditionalHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly)
            .AddHandlersFromAssemblies(typeof(PingRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var handler = provider.GetService<IRequestHandler<PingRequest, string>>();

        // Assert
        handler.ShouldNotBeNull();
    }


    [Fact]
    public void AddBehavior_Type_ClosedGeneric_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly)
            .AddBehavior(typeof(ClosedPingValidationBehavior));
        var provider = services.BuildServiceProvider();

        // Act
        var behaviors = provider.GetServices<IPipelineBehavior<PingRequest, string>>().ToList();

        // Assert
        behaviors.Count.ShouldBe(1);
        behaviors[0].ShouldBeOfType<ClosedPingValidationBehavior>();
    }

    [Fact]
    public void AddBehavior_Generic_TypeWithNonGenericInterface_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = services.AddCommandFlow(typeof(PingRequestHandler).Assembly);

        // Act & Assert — NonGenericInterfaceType implements IMarkerInterface (non-generic),
        // exercising the i.IsGenericType == false branch in the interface scan
        Should.Throw<ArgumentException>(() => config.AddBehavior<NonGenericInterfaceType>());
    }

    [Fact]
    public void AddBehavior_Type_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = services.AddCommandFlow(typeof(PingRequestHandler).Assembly);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => config.AddBehavior(null!));
    }

    [Fact]
    public void AddCommandFlow_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddCommandFlow(typeof(PingRequestHandler).Assembly));
    }

    [Fact]
    public void AddHandlersFromAssemblies_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = services.AddCommandFlow(typeof(PingRequestHandler).Assembly);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => config.AddHandlersFromAssemblies(null!));
    }
}
