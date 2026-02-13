using Microsoft.Extensions.DependencyInjection;
using Serilog;
using AirDropWindows.Services.Configuration;

namespace AirDropWindows.Services.Extensions;

/// <summary>
/// Extension methods for registering services with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all AirDrop Windows services
    /// </summary>
    public static IServiceCollection AddAirDropServices(this IServiceCollection services, ILogger logger)
    {
        // Register logger
        services.AddSingleton(logger);

        // Register configuration service
        services.AddSingleton<ConfigurationService>();

        // TODO: Register discovery services
        // services.AddSingleton<IDiscoveryService, DiscoveryService>();

        // TODO: Register network services
        // services.AddSingleton<INetworkTransport, WiFiDirectTransport>();

        // TODO: Register security services
        // services.AddSingleton<ISecurityService, SecurityService>();

        // TODO: Register transfer services
        // services.AddSingleton<ITransferService, TransferService>();

        return services;
    }
}
