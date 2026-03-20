# AGENTS.md — CommandFlow

## Project Overview

CommandFlow is a lightweight in-process CQRS/mediator library for .NET 10. It has **zero external runtime dependencies** — the core `CommandFlow` package is pure .NET with no NuGet references. DI integration lives in a separate package.

## Architecture

Two NuGet-packable projects with a clear dependency direction:

- **`src/CommandFlow/`** — Core abstractions and `Mediator` implementation. No DI references. All dispatch uses reflection-based invocation (`MethodInfo.Invoke`) with `TargetInvocationException` unwrapping.
- **`src/CommandFlow.DependencyInjection/`** — `IServiceCollection` extensions (`AddCommandFlow`) and fluent `CommandFlowConfiguration`. Depends on core. Namespace is `Microsoft.Extensions.DependencyInjection` (intentional — enables discovery without extra `using`).

### Type Hierarchy (important to get right)

```
IRequest<TResponse>
├── ICommand         → IRequest<Unit>     (void commands)
├── ICommand<T>      → IRequest<T>        (commands with result)
└── IQuery<T>        → IRequest<T>        (queries)

IRequestHandler<TRequest, TResponse>
└── ICommandHandler<TCommand>             (has HandleCommand → bridges to Handle via default interface method)
```

`ICommandHandler<T>` uses a **default interface method** to adapt `HandleCommand` → `IRequestHandler.Handle`, returning `Unit.Value`. When implementing void commands, implement `ICommandHandler<T>.HandleCommand`, not `Handle`.

### Key Design Decisions

- **Notifications execute sequentially** in registration order. If one throws, remaining handlers do NOT run.
- **Pipeline behaviors** wrap handlers as middleware. First-registered behavior is **outermost** (reversed during chain construction in `Mediator.Send`).
- **`IMediator`** combines `ISender` + `IPublisher`. All three are registered in DI; `ISender`/`IPublisher` forward to `IMediator`.
- All DI registrations are **Transient** by default. Behaviors support configurable `ServiceLifetime`.

## Build & Test Commands

```bash
dotnet build              # Build all projects
dotnet test               # Run unit tests (xunit)
dotnet test --settings coverage.runsettings --collect:"XPlat Code Coverage"  # Run tests with coverage
dotnet run --project benchmarks/CommandFlow.Benchmarks  # Run BenchmarkDotNet suite
```

Solution file: `CommandFlow.slnx` (new XML-based format). Requires .NET 10 SDK (`global.json`).

### Code Coverage

Coverage is collected via **coverlet.collector** and configured in `coverage.runsettings`. The runsettings file excludes `[CommandFlow.Benchmarks]*` and `[CommandFlow.UnitTests]*` — only `CommandFlow` and `CommandFlow.DependencyInjection` assemblies are measured. Target is **100% line and branch coverage**.

## Conventions

- **Central package management**: all NuGet versions in `Directory.Packages.props` — never specify versions in `.csproj` files
- **`Directory.Build.props`**: enforces `TreatWarningsAsErrors`, `Nullable enable`, `LangVersion latest` across all projects
- **Test framework**: xunit + Shouldly (assertions) + Moq. Global `<Using Include="Xunit" />` in test csproj — no need to add `using Xunit;`
- **Test fakes**: all shared test types (commands, queries, handlers, behaviors) live in `tests/CommandFlow.UnitTests/Fakes/TestTypes.cs` — add new fakes there, not inline
- **Test pattern**: each test creates a fresh `ServiceCollection`, registers handlers, builds provider, resolves `IMediator` — no shared fixtures
- **AAA structure**: every test must follow **Arrange-Act-Assert** with explicit `// Arrange`, `// Act`, and `// Assert` comments
- **Records for messages**: all commands, queries, and notifications are `record` types
- **Behaviors**: open generics (`LoggingBehavior<,>`) registered via `.AddBehavior(typeof(Behavior<,>))`; closed generics via `.AddBehavior<SpecificBehavior>()`

## Adding a New Feature

**New request type**: Define a `record` implementing `ICommand`/`ICommand<T>`/`IQuery<T>`, create a handler implementing `IRequestHandler<,>` or `ICommandHandler<>`, add test fakes to `Fakes/TestTypes.cs`, write tests in the corresponding `*Tests.cs` file.

**New pipeline behavior**: Implement `IPipelineBehavior<TRequest, TResponse>`, call `next()` to continue or skip to short-circuit. Register with `.AddBehavior(typeof(MyBehavior<,>))`.

## File Reference

| Purpose | Path |
|---------|------|
| Core interfaces | `src/CommandFlow/IRequest.cs`, `IRequestHandler.cs`, `INotification.cs`, `IPipelineBehavior.cs` |
| Mediator (all dispatch logic) | `src/CommandFlow/Mediator.cs` |
| DI registration & scanning | `src/CommandFlow.DependencyInjection/ServiceCollectionExtensions.cs` |
| Fluent config (behaviors) | `src/CommandFlow.DependencyInjection/CommandFlowConfiguration.cs` |
| Test fakes | `tests/CommandFlow.UnitTests/Fakes/TestTypes.cs` |
| Coverage config | `coverage.runsettings` |
| CI workflow | `.github/workflows/ci.yml` |
| Release workflow | `.github/workflows/release.yml` |
| Benchmarks | `benchmarks/CommandFlow.Benchmarks/` |

