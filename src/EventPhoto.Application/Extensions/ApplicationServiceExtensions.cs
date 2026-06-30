using EventPhoto.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EventPhoto.Application.Extensions;

/// <summary>DI registration extensions for the Application layer.</summary>
public static class ApplicationServiceExtensions
{
    /// <summary>Registers MediatR handlers, FluentValidation validators, AutoMapper profiles, and pipeline behaviors.</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly));
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceExtensions).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ApplicationServiceExtensions).Assembly));
        return services;
    }
}
