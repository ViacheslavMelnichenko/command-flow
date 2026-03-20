using CommandFlow.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CommandFlow.UnitTests;

public class QueryTests
{
    [Fact]
    public async Task Send_Query_ReturnsExpectedResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(GetOrderByIdQueryHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new GetOrderByIdQuery(7));

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(7);
        result.ProductName.ShouldBe("Widget");
        result.Quantity.ShouldBe(5);
    }

    [Fact]
    public async Task Send_Query_FlowsCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(GetOrderByIdQueryHandler).Assembly)
            .AddBehavior(typeof(CancellationCheckBehavior<,>));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.Send(new GetOrderByIdQuery(1), cts.Token);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }
}
