# DI Registration

CommandFlow integrates with `Microsoft.Extensions.DependencyInjection`.

## Basic setup

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCommandFlow(typeof(MyHandler).Assembly);
```

This registers:
- `IMediator` as transient
- `ISender` as transient (resolves to `IMediator`)
- `IPublisher` as transient (resolves to `IMediator`)
- All `IRequestHandler<,>` implementations found in the assembly
- All `INotificationHandler<>` implementations found in the assembly

## Adding pipeline behaviors

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddBehavior(typeof(LoggingBehavior<,>))
    .AddBehavior(typeof(ValidationBehavior<,>));
```

Behaviors are registered in order — the first is outermost.

## Scanning multiple assemblies

```csharp
services.AddCommandFlow(
    typeof(HandlerInAssembly1).Assembly,
    typeof(HandlerInAssembly2).Assembly
);
```

Or add more assemblies later:

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddHandlersFromAssemblies(typeof(PluginHandler).Assembly);
```

## Service lifetimes

| Registration | Default Lifetime |
|---|---|
| `IMediator` | Transient |
| `ISender` | Transient |
| `IPublisher` | Transient |
| Request handlers | Transient |
| Notification handlers | Transient |
| Pipeline behaviors | Transient (configurable) |

### Custom behavior lifetime

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddBehavior(typeof(CacheBehavior<,>), ServiceLifetime.Singleton);
```

## Fluent configuration API

`AddCommandFlow` returns a `CommandFlowConfiguration` object that supports chaining:

```csharp
services.AddCommandFlow(typeof(MyHandler).Assembly)
    .AddBehavior(typeof(LoggingBehavior<,>))
    .AddBehavior(typeof(ValidationBehavior<,>))
    .AddHandlersFromAssemblies(typeof(PluginHandler).Assembly);
```

## ASP.NET Core integration

In a typical ASP.NET Core app:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommandFlow(typeof(Program).Assembly)
    .AddBehavior(typeof(LoggingBehavior<,>));

var app = builder.Build();

app.MapGet("/users/{id}", async (int id, IMediator mediator) =>
{
    var user = await mediator.Send(new GetUserByIdQuery(id));
    return Results.Ok(user);
});

app.Run();
```

