using AirDropWindows.Core.Models;
using Serilog;
using System.Collections.Concurrent;

namespace AirDropWindows.Discovery;

/// <summary>
/// Maintains registry of discovered devices with timeout handling
/// </summary>
public class DeviceRegistry
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, DiscoveredDevice> _devices;
    private readonly TimeSpan _deviceExpirationTime;
    private readonly System.Timers.Timer _cleanupTimer;

    public event EventHandler<DiscoveredDevice>? DeviceAdded;
    public event EventHandler<DiscoveredDevice>? DeviceUpdated;
    public event EventHandler<DiscoveredDevice>? DeviceRemoved;

    public DeviceRegistry(ILogger logger, TimeSpan deviceExpirationTime)
    {
        _logger = logger;
        _devices = new ConcurrentDictionary<string, DiscoveredDevice>();
        _deviceExpirationTime = deviceExpirationTime;

        // Setup cleanup timer to remove expired devices
        _cleanupTimer = new System.Timers.Timer(10000); // Check every 10 seconds
        _cleanupTimer.Elapsed += OnCleanupTimer;
        _cleanupTimer.Start();
    }

    /// <summary>
    /// Add or update a device in the registry
    /// </summary>
    public void AddOrUpdateDevice(DiscoveredDevice device)
    {
        var isNew = !_devices.ContainsKey(device.DeviceId);

        _devices.AddOrUpdate(
            device.DeviceId,
            // Add new device
            key =>
            {
                _logger.Information("New device added to registry: {DeviceId} ({DeviceName})",
                    device.DeviceId, device.DeviceName);
                DeviceAdded?.Invoke(this, device);
                return device;
            },
            // Update existing device
            (key, existingDevice) =>
            {
                // Merge information from both sources
                var updated = MergeDeviceInfo(existingDevice, device);
                updated.LastSeenAt = DateTime.UtcNow;
                updated.IsAvailable = true;

                _logger.Debug("Device updated in registry: {DeviceId} ({DeviceName})",
                    device.DeviceId, device.DeviceName);
                DeviceUpdated?.Invoke(this, updated);
                return updated;
            });
    }

    /// <summary>
    /// Merge device information from multiple sources
    /// </summary>
    private DiscoveredDevice MergeDeviceInfo(DiscoveredDevice existing, DiscoveredDevice newInfo)
    {
        return new DiscoveredDevice
        {
            DeviceId = existing.DeviceId,
            DeviceName = string.IsNullOrEmpty(existing.DeviceName) ? newInfo.DeviceName : existing.DeviceName,
            DeviceType = existing.DeviceType != DeviceType.Unknown ? existing.DeviceType : newInfo.DeviceType,
            IpAddress = newInfo.IpAddress ?? existing.IpAddress,
            Port = newInfo.Port != 0 ? newInfo.Port : existing.Port,
            DiscoveredAt = existing.DiscoveredAt,
            LastSeenAt = DateTime.UtcNow,
            IsAvailable = true,
            ConnectionState = existing.ConnectionState,
            IdentityRecord = newInfo.IdentityRecord ?? existing.IdentityRecord,
            Metadata = MergeMetadata(existing.Metadata, newInfo.Metadata)
        };
    }

    /// <summary>
    /// Merge metadata dictionaries
    /// </summary>
    private Dictionary<string, string> MergeMetadata(
        Dictionary<string, string> existing,
        Dictionary<string, string> newMetadata)
    {
        var merged = new Dictionary<string, string>(existing);

        foreach (var kvp in newMetadata)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    /// <summary>
    /// Remove a device from the registry
    /// </summary>
    public void RemoveDevice(string deviceId)
    {
        if (_devices.TryRemove(deviceId, out var device))
        {
            _logger.Information("Device removed from registry: {DeviceId} ({DeviceName})",
                deviceId, device.DeviceName);
            device.IsAvailable = false;
            DeviceRemoved?.Invoke(this, device);
        }
    }

    /// <summary>
    /// Get a specific device by ID
    /// </summary>
    public DiscoveredDevice? GetDevice(string deviceId)
    {
        return _devices.TryGetValue(deviceId, out var device) ? device : null;
    }

    /// <summary>
    /// Get all discovered devices
    /// </summary>
    public IReadOnlyCollection<DiscoveredDevice> GetAllDevices()
    {
        return _devices.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Get only available devices (not expired)
    /// </summary>
    public IReadOnlyCollection<DiscoveredDevice> GetAvailableDevices()
    {
        return _devices.Values
            .Where(d => d.IsAvailable && !IsDeviceExpired(d))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Update device connection state
    /// </summary>
    public void UpdateDeviceConnectionState(string deviceId, ConnectionState state)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            device.ConnectionState = state;
            device.LastSeenAt = DateTime.UtcNow;

            _logger.Debug("Device {DeviceId} connection state updated to {State}",
                deviceId, state);

            DeviceUpdated?.Invoke(this, device);
        }
    }

    /// <summary>
    /// Check if device has expired
    /// </summary>
    private bool IsDeviceExpired(DiscoveredDevice device)
    {
        var timeSinceLastSeen = DateTime.UtcNow - device.LastSeenAt;
        return timeSinceLastSeen > _deviceExpirationTime;
    }

    /// <summary>
    /// Cleanup timer to remove expired devices
    /// </summary>
    private void OnCleanupTimer(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            var expiredDevices = _devices.Values
                .Where(IsDeviceExpired)
                .ToList();

            foreach (var device in expiredDevices)
            {
                _logger.Debug("Device expired: {DeviceId} ({DeviceName}), last seen: {LastSeen}",
                    device.DeviceId, device.DeviceName, device.LastSeenAt);
                RemoveDevice(device.DeviceId);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during device registry cleanup");
        }
    }

    /// <summary>
    /// Clear all devices from registry
    /// </summary>
    public void Clear()
    {
        _logger.Information("Clearing device registry");
        _devices.Clear();
    }

    public void Dispose()
    {
        _cleanupTimer.Stop();
        _cleanupTimer.Dispose();
        Clear();
    }
}
