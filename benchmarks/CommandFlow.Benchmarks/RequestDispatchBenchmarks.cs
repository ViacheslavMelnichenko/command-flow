using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CommandFlow;
using Microsoft.Extensions.DependencyInjection;

namespace CommandFlow.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class RequestDispatchBenchmarks
{
    private IMediator _mediator = null!;
    private IMediator _mediatorWithPipeline = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddCommandFlow(typeof(RequestDispatchBenchmarks).Assembly);
        _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var pipelineServices = new ServiceCollection();
        pipelineServices.AddCommandFlow(typeof(RequestDispatchBenchmarks).Assembly)
            .AddBehavior(typeof(NoOpBehavior<,>));
        _mediatorWithPipeline = pipelineServices.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Benchmark(Baseline = true)]
    public Task<string> SendRequest()
    {
        return _mediator.Send(new BenchmarkPing());
    }

    [Benchmark]
    public Task<string> SendRequestWithPipeline()
    {
        return _mediatorWithPipeline.Send(new BenchmarkPing());
    }

    [Benchmark]
    public Task<Unit> SendVoidCommand()
    {
        return _mediator.Send(new BenchmarkCommand());
    }
}

// --- Benchmark types ---

public record BenchmarkPing : IRequest<string>;

public class BenchmarkPingHandler : IRequestHandler<BenchmarkPing, string>
{
    public Task<string> Handle(BenchmarkPing request, CancellationToken cancellationToken)
        => Task.FromResult("Pong");
}

public record BenchmarkCommand : ICommand;

public class BenchmarkCommandHandler : ICommandHandler<BenchmarkCommand>
{
    public Task HandleCommand(BenchmarkCommand command, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class NoOpBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        => next();
}

