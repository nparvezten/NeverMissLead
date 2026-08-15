using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NeverMissLead.Application.Common.Mediator;

namespace NeverMissLead.Application.Extensions;

/// <summary>
/// Registers Application-layer services: native mediator, handlers (assembly-scanned),
/// and FluentValidation validators.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceExtensions).Assembly;

        // Register all IRequestHandler<,> implementations from this assembly
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => (HandlerType: t, Interface: i)));

        foreach (var (handlerType, interfaceType) in handlerTypes)
            services.AddScoped(interfaceType, handlerType);

        // Register mediator
        services.AddScoped<IMediator, Mediator>();

        // Register FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(assembly, lifetime: ServiceLifetime.Scoped);

        return services;
    }
}
