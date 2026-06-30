using EventPhoto.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventPhoto.Infrastructure;

/// <summary>Extension methods for registering Infrastructure-layer services with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Delegates to <see cref="InfrastructureServiceExtensions.AddInfrastructureServices"/>.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        => services.AddInfrastructureServices(configuration);
}
