using System.Reflection;
using CommandFlow;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Fluent configuration for CommandFlow services.
/// </summary>
public sealed class CommandFlowConfiguration
{
    private readonly IServiceCollection _services;

    internal CommandFlowConfiguration(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers a closed-generic pipeline behavior. Behaviors are executed in the order they are registered.
    /// The first registered behavior is the outermost wrapper around the handler.
    /// For open-generic behaviors, use <see cref="AddBehavior(Type, ServiceLifetime)"/> instead.
    /// </summary>
    /// <param name="lifetime">The service lifetime for the behavior. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <typeparam name="TBehavior">The closed-generic pipeline behavior type.</typeparam>
    /// <returns>This configuration instance for chaining.</returns>
    public CommandFlowConfiguration AddBehavior<TBehavior>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TBehavior : class
    {
        var behaviorType = typeof(TBehavior);

        var pipelineInterface = behaviorType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            ?? throw new ArgumentException(
                $"{behaviorType.Name} does not implement IPipelineBehavior<TRequest, TResponse>.",
                nameof(TBehavior));

        _services.Add(new ServiceDescriptor(pipelineInterface, behaviorType, lifetime));

        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior type. Behaviors are executed in the order they are registered.
    /// </summary>
    /// <param name="behaviorType">The pipeline behavior type (open or closed generic).</param>
    /// <param name="lifetime">The service lifetime for the behavior. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This configuration instance for chaining.</returns>
    public CommandFlowConfiguration AddBehavior(Type behaviorType, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        if (behaviorType.IsGenericTypeDefinition)
        {
            _services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, lifetime));
        }
        else
        {
            var pipelineInterface = behaviorType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                ?? throw new ArgumentException(
                    $"{behaviorType.Name} does not implement IPipelineBehavior<TRequest, TResponse>.",
                    nameof(behaviorType));

            _services.Add(new ServiceDescriptor(pipelineInterface, behaviorType, lifetime));
        }

        return this;
    }

    /// <summary>
    /// Scans additional assemblies for handlers and registers them.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>This configuration instance for chaining.</returns>
    public CommandFlowConfiguration AddHandlersFromAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var handlerInterfaces = new[]
        {
            typeof(IRequestHandler<,>),
            typeof(INotificationHandler<>),
        };

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition()));

                foreach (var @interface in interfaces)
                {
                    _services.AddTransient(@interface, type);
                }
            }
        }

        return this;
    }
}

