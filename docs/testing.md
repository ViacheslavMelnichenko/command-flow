# Testing

CommandFlow is designed to be easy to test at every level.

## Testing handlers directly

The simplest approach — no DI, no mediator, just call the handler:

```csharp
[Fact]
public async Task GetUserQueryHandler_ReturnsUser()
{
    var handler = new GetUserQueryHandler(new FakeUserRepository());

    var result = await handler.Handle(new GetUserQuery(1), CancellationToken.None);

    Assert.Equal("Alice", result.Name);
}
```

## Mocking the mediator

When testing code that depends on `IMediator`, mock it:

```csharp
[Fact]
public async Task Controller_ReturnsUser()
{
    var mediator = new Mock<IMediator>();
    mediator
        .Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new UserDto(1, "Alice"));

    var controller = new UserController(mediator.Object);

    var result = await controller.GetUser(1);

    Assert.Equal("Alice", result.Name);
}
```

## Mocking ISender or IPublisher

If your code only depends on `ISender` or `IPublisher`, mock that specific interface:

```csharp
[Fact]
public async Task Service_SendsCommand()
{
    var sender = new Mock<ISender>();
    var service = new OrderService(sender.Object);

    await service.PlaceOrder("Widget", 3);

    sender.Verify(s => s.Send(
        It.Is<CreateOrderCommand>(c => c.ProductName == "Widget"),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

## Integration testing with real DI

Build a real service provider for end-to-end tests:

```csharp
[Fact]
public async Task FullPipeline_WithBehaviors()
{
    var services = new ServiceCollection();
    services.AddCommandFlow(typeof(CreateUserHandler).Assembly)
        .AddBehavior(typeof(ValidationBehavior<,>));

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    var result = await mediator.Send(new CreateUserCommand("Alice"));

    Assert.True(result > 0);
}
```

## Testing pipeline behaviors

Test behaviors in isolation:

```csharp
[Fact]
public async Task LoggingBehavior_CallsNext()
{
    var behavior = new LoggingBehavior<PingRequest, string>(Mock.Of<ILogger<LoggingBehavior<PingRequest, string>>>());
    var nextCalled = false;

    var result = await behavior.Handle(
        new PingRequest(),
        () =>
        {
            nextCalled = true;
            return Task.FromResult("Pong");
        },
        CancellationToken.None);

    Assert.True(nextCalled);
    Assert.Equal("Pong", result);
}
```

## Testing notifications

Verify that publishing invokes all handlers:

```csharp
[Fact]
public async Task Publish_InvokesAllHandlers()
{
    var handler1Called = false;
    var handler2Called = false;

    var services = new ServiceCollection();
    services.AddTransient<IMediator, Mediator>();
    services.AddTransient<INotificationHandler<OrderPlacedEvent>>(_ =>
        new DelegateHandler(() => handler1Called = true));
    services.AddTransient<INotificationHandler<OrderPlacedEvent>>(_ =>
        new DelegateHandler(() => handler2Called = true));

    var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
    await mediator.Publish(new OrderPlacedEvent(1));

    Assert.True(handler1Called);
    Assert.True(handler2Called);
}
```

## Testing cancellation

```csharp
[Fact]
public async Task Handler_RespectssCancellation()
{
    var cts = new CancellationTokenSource();
    cts.Cancel();

    var services = new ServiceCollection();
    services.AddCommandFlow(typeof(MyHandler).Assembly)
        .AddBehavior(typeof(CancellationCheckBehavior<,>));
    var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

    await Assert.ThrowsAsync<OperationCanceledException>(
        () => mediator.Send(new MyRequest(), cts.Token));
}
```

