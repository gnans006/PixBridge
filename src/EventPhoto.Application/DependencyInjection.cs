using EventPhoto.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace EventPhoto.Application;

/// <summary>Extension methods for registering Application-layer services with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Registers all application services. Delegates to <see cref="ApplicationServiceExtensions.AddApplicationServices"/>.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services.AddApplicationServices();
}
