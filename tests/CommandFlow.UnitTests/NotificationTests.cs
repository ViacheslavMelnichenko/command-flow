using CommandFlow.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace CommandFlow.UnitTests;

public class NotificationTests
{
    [Fact]
    public async Task Publish_Notification_InvokesAllHandlers()
    {
        // Arrange
        var emailHandler = new OrderCreatedEmailHandler();
        var loggingHandler = new OrderCreatedLoggingHandler();
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<INotificationHandler<OrderCreatedNotification>>(_ => emailHandler);
        services.AddTransient<INotificationHandler<OrderCreatedNotification>>(_ => loggingHandler);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Publish(new OrderCreatedNotification(1, "Widget"));

        // Assert
        emailHandler.WasCalled.ShouldBeTrue();
        loggingHandler.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Publish_Notification_HandlersExecuteSequentially()
    {
        // Arrange
        var callOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<INotificationHandler<OrderCreatedNotification>>(_ =>
            new DelegateNotificationHandler<OrderCreatedNotification>((_, _) =>
            {
                callOrder.Add("first");
                return Task.CompletedTask;
            }));
        services.AddTransient<INotificationHandler<OrderCreatedNotification>>(_ =>
            new DelegateNotificationHandler<OrderCreatedNotification>((_, _) =>
            {
                callOrder.Add("second");
                return Task.CompletedTask;
            }));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Publish(new OrderCreatedNotification(1, "Widget"));

        // Assert
        callOrder.ShouldBe(new[] { "first", "second" });
    }

    [Fact]
    public async Task Publish_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert — no handlers registered, should complete without error
        await mediator.Publish(new OrderCreatedNotification(1, "Widget"));
    }

    [Fact]
    public async Task Publish_WhenHandlerThrows_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<INotificationHandler<OrderCreatedNotification>, ThrowingNotificationHandler>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.Publish(new OrderCreatedNotification(1, "Widget"));

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task Publish_NullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.Publish<OrderCreatedNotification>(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task Publish_ViaIPublisher_Works()
    {
        // Arrange
        var emailHandler = new OrderCreatedEmailHandler();
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());
        services.AddTransient<INotificationHandler<OrderCreatedNotification>>(_ => emailHandler);
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IPublisher>();

        // Act
        await publisher.Publish(new OrderCreatedNotification(1, "Widget"));

        // Assert
        emailHandler.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Publish_FlowsCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        CancellationToken receivedToken = default;
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<INotificationHandler<OrderCreatedNotification>>(_ =>
            new DelegateNotificationHandler<OrderCreatedNotification>((_, ct) =>
            {
                receivedToken = ct;
                return Task.CompletedTask;
            }));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Publish(new OrderCreatedNotification(1, "Widget"), cts.Token);

        // Assert
        receivedToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task Publish_WhenServiceProviderReturnsNullHandlers_DoesNotThrow()
    {
        // Arrange
        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(sp => sp.GetService(It.IsAny<Type>())).Returns(null!);
        var mediator = new Mediator(mockProvider.Object);

        // Act & Assert — null from GetService should be handled gracefully
        await mediator.Publish(new OrderCreatedNotification(1, "Widget"));
    }

    private class DelegateNotificationHandler<T>(Func<T, CancellationToken, Task> handler)
        : INotificationHandler<T> where T : INotification
    {
        public Task Handle(T notification, CancellationToken cancellationToken) =>
            handler(notification, cancellationToken);
    }
}
