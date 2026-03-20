using CommandFlow.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CommandFlow.UnitTests;

public class CommandTests
{
    [Fact]
    public async Task Send_VoidCommand_InvokesHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(CreateOrderCommandHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new CreateOrderCommand("Widget", 3);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.ShouldBe(Unit.Value);
    }

    [Fact]
    public async Task Send_CommandWithResult_ReturnsExpectedValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(CreateOrderWithResultCommandHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new CreateOrderWithResultCommand("Widget", 3);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.ShouldBe(42);
    }

    [Fact]
    public async Task Send_VoidCommand_FlowsCancellationToken()
    {
        // Arrange
        var handler = new CreateOrderCommandHandler();
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<CreateOrderCommand, Unit>>(_ => handler);
        services.AddTransient<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await mediator.Send(new CreateOrderCommand("Widget", 1), token);

        // Assert
        handler.ReceivedToken.ShouldBe(token);
    }
}
