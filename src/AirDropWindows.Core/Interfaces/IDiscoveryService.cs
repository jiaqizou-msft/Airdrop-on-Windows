using AirDropWindows.Core.Models;

namespace AirDropWindows.Core.Interfaces;

/// <summary>
/// Service for discovering AirDrop-compatible devices
/// </summary>
public interface IDiscoveryService
{
    /// <summary>
    /// Event raised when a new device is discovered
    /// </summary>
    event EventHandler<DiscoveredDevice>? DeviceDiscovered;

    /// <summary>
    /// Event raised when a device is no longer available
    /// </summary>
    event EventHandler<DiscoveredDevice>? DeviceLost;

    /// <summary>
    /// Event raised when a device's information is updated
    /// </summary>
    event EventHandler<DiscoveredDevice>? DeviceUpdated;

    /// <summary>
    /// Start the discovery service
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the discovery service
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all currently discovered devices
    /// </summary>
    IReadOnlyCollection<DiscoveredDevice> GetDiscoveredDevices();

    /// <summary>
    /// Get a specific device by ID
    /// </summary>
    DiscoveredDevice? GetDevice(string deviceId);

    /// <summary>
    /// Manually trigger a discovery scan
    /// </summary>
    Task ScanAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the service is currently running
    /// </summary>
    bool IsRunning { get; }
}
