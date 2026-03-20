using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CommandFlow;
using Microsoft.Extensions.DependencyInjection;

namespace CommandFlow.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class NotificationPublishBenchmarks
{
    private IMediator _mediatorSingle = null!;
    private IMediator _mediatorMultiple = null!;

    [GlobalSetup]
    public void Setup()
    {
        var singleServices = new ServiceCollection();
        singleServices.AddTransient<IMediator, Mediator>();
        singleServices.AddTransient<INotificationHandler<BenchmarkEvent>, BenchmarkEventHandler1>();
        _mediatorSingle = singleServices.BuildServiceProvider().GetRequiredService<IMediator>();

        var multiServices = new ServiceCollection();
        multiServices.AddTransient<IMediator, Mediator>();
        multiServices.AddTransient<INotificationHandler<BenchmarkEvent>, BenchmarkEventHandler1>();
        multiServices.AddTransient<INotificationHandler<BenchmarkEvent>, BenchmarkEventHandler2>();
        multiServices.AddTransient<INotificationHandler<BenchmarkEvent>, BenchmarkEventHandler3>();
        _mediatorMultiple = multiServices.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Benchmark(Baseline = true)]
    public Task PublishToSingleHandler()
    {
        return _mediatorSingle.Publish(new BenchmarkEvent());
    }

    [Benchmark]
    public Task PublishToThreeHandlers()
    {
        return _mediatorMultiple.Publish(new BenchmarkEvent());
    }
}

// --- Benchmark types ---

public record BenchmarkEvent : INotification;

public class BenchmarkEventHandler1 : INotificationHandler<BenchmarkEvent>
{
    public Task Handle(BenchmarkEvent notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class BenchmarkEventHandler2 : INotificationHandler<BenchmarkEvent>
{
    public Task Handle(BenchmarkEvent notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class BenchmarkEventHandler3 : INotificationHandler<BenchmarkEvent>
{
    public Task Handle(BenchmarkEvent notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

