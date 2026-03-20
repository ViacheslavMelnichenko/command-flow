using CommandFlow.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CommandFlow.UnitTests;

public class RequestTests
{
    [Fact]
    public async Task Send_GenericRequest_ReturnsExpectedResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PingRequest());

        // Assert
        result.ShouldBe("Pong");
    }

    [Fact]
    public async Task Send_UnregisteredRequest_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.Send(new PingRequest());

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.Send<string>(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task Send_ViaISender_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(PingRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        // Act
        var result = await sender.Send(new PingRequest());

        // Assert
        result.ShouldBe("Pong");
    }

    [Fact]
    public void Mediator_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new Mediator(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task Send_WhenHandlerThrowsSynchronously_UnwrapsTargetInvocationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<IRequestHandler<ThrowingRequest, string>, ThrowingRequestHandler>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.Send(new ThrowingRequest());

        // Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(act);
        ex.Message.ShouldBe("Handler exploded");
    }
}
