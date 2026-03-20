using System.Reflection;
using CommandFlow;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering CommandFlow services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds CommandFlow services to the specified <see cref="IServiceCollection"/>.
    /// Scans the provided assemblies for handlers, and registers them along with the mediator.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="assemblies">The assemblies to scan for handlers and behaviors.</param>
    /// <returns>The <see cref="CommandFlowConfiguration"/> for further configuration.</returns>
    public static CommandFlowConfiguration AddCommandFlow(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (assemblies.Length == 0)
            throw new ArgumentException("At least one assembly must be provided for scanning.", nameof(assemblies));

        var configuration = new CommandFlowConfiguration(services);

        // Register the mediator
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        // Scan and register handlers
        RegisterHandlers(services, assemblies);

        return configuration;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
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
                    services.AddTransient(@interface, type);
                }
            }
        }
    }
}

