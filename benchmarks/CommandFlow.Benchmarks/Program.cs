using BenchmarkDotNet.Running;
using CommandFlow.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(RequestDispatchBenchmarks).Assembly).Run(args);

