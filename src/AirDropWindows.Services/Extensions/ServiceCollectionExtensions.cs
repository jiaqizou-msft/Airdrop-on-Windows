using Microsoft.Extensions.DependencyInjection;
using Serilog;
using AirDropWindows.Services.Configuration;
using AirDropWindows.Core.Configuration;
using AirDropWindows.Core.Models;

namespace AirDropWindows.Services.Extensions;

/// <summary>
/// Extension methods for registering services with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all AirDrop Windows services
    /// </summary>
    public static IServiceCollection AddAirDropServices(this IServiceCollection services, ILogger logger, AppSettings settings)
    {
        // Register logger
        services.AddSingleton(logger);

        // Register configuration and settings
        services.AddSingleton(settings);
        services.AddSingleton(settings.Security);
        services.AddSingleton<ConfigurationService>();

        // Create and register device identity from settings
        var discoveryMode = Enum.TryParse<DiscoveryMode>(settings.Device.DiscoveryMode, out var mode)
            ? mode
            : DiscoveryMode.Everyone;

        var deviceIdentity = new DeviceIdentity
        {
            DeviceId = settings.Device.DeviceId,
            DeviceName = settings.Device.DeviceName,
            DeviceType = DeviceType.WindowsPC,
            DiscoveryMode = discoveryMode,
            AutoAccept = settings.Device.AutoAccept,
            DefaultSaveLocation = settings.Device.DefaultSaveLocation
        };
        services.AddSingleton(deviceIdentity);

        // Register discovery services
        services.AddSingleton<AirDropWindows.Core.Interfaces.IDiscoveryService, AirDropWindows.Discovery.DiscoveryService>();

        // Register security services
        services.AddSingleton<AirDropWindows.Core.Interfaces.ISecurityService, AirDropWindows.Security.SecurityService>();

        // Register transfer services
        services.AddSingleton<AirDropWindows.Core.Interfaces.ITransferService, TransferService>();

        return services;
    }
}
