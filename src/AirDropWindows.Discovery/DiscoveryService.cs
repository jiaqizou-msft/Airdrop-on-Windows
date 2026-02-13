using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Models;
using AirDropWindows.Core.Configuration;
using AirDropWindows.Discovery.BLE;
using AirDropWindows.Discovery.mDNS;
using Serilog;

namespace AirDropWindows.Discovery;

/// <summary>
/// Coordinates BLE and mDNS discovery to find AirDrop devices
/// </summary>
public class DiscoveryService : IDiscoveryService, IDisposable
{
    private readonly ILogger _logger;
    private readonly DeviceIdentity _deviceIdentity;
    private readonly AppSettings _settings;
    private readonly DeviceRegistry _deviceRegistry;
    private readonly BleAdvertisementScanner _bleScanner;
    private readonly BleAdvertisementPublisher _blePublisher;
    private readonly MdnsServiceBrowser _mdnsBrowser;
    private readonly MdnsServicePublisher _mdnsPublisher;

    public event EventHandler<DiscoveredDevice>? DeviceDiscovered;
    public event EventHandler<DiscoveredDevice>? DeviceLost;
    public event EventHandler<DiscoveredDevice>? DeviceUpdated;

    public bool IsRunning { get; private set; }

    public DiscoveryService(
        ILogger logger,
        DeviceIdentity deviceIdentity,
        AppSettings settings)
    {
        _logger = logger;
        _deviceIdentity = deviceIdentity;
        _settings = settings;

        // Create device registry
        var expirationTime = TimeSpan.FromSeconds(settings.Network.DeviceExpirationSeconds);
        _deviceRegistry = new DeviceRegistry(logger, expirationTime);

        // Wire up registry events
        _deviceRegistry.DeviceAdded += OnDeviceAdded;
        _deviceRegistry.DeviceUpdated += OnDeviceUpdated;
        _deviceRegistry.DeviceRemoved += OnDeviceRemoved;

        // Create BLE components
        _bleScanner = new BleAdvertisementScanner(logger);
        _blePublisher = new BleAdvertisementPublisher(logger, deviceIdentity);

        // Wire up BLE events
        _bleScanner.DeviceDiscovered += OnBleDeviceDiscovered;

        // Create mDNS components
        _mdnsBrowser = new MdnsServiceBrowser(logger);
        _mdnsPublisher = new MdnsServicePublisher(logger, deviceIdentity, settings.Network.Port);

        // Wire up mDNS events
        _mdnsBrowser.ServiceDiscovered += OnMdnsServiceDiscovered;
        _mdnsBrowser.ServiceRemoved += OnMdnsServiceRemoved;
    }

    /// <summary>
    /// Start discovery service
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.Warning("Discovery service is already running");
            return;
        }

        try
        {
            _logger.Information("Starting discovery service");

            // Start BLE components
            if (_settings.Device.DiscoveryMode != "Off")
            {
                _blePublisher.Start();
            }
            _bleScanner.Start();

            // Start mDNS components
            if (_settings.Device.DiscoveryMode != "Off")
            {
                _mdnsPublisher.Start();
            }
            _mdnsBrowser.Start();

            IsRunning = true;
            _logger.Information("Discovery service started successfully");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start discovery service");
            throw;
        }
    }

    /// <summary>
    /// Stop discovery service
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            _logger.Information("Stopping discovery service");

            // Stop BLE components
            _bleScanner.Stop();
            _blePublisher.Stop();

            // Stop mDNS components
            _mdnsBrowser.Stop();
            _mdnsPublisher.Stop();

            IsRunning = false;
            _logger.Information("Discovery service stopped");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error stopping discovery service");
        }
    }

    /// <summary>
    /// Get all currently discovered devices
    /// </summary>
    public IReadOnlyCollection<DiscoveredDevice> GetDiscoveredDevices()
    {
        return _deviceRegistry.GetAvailableDevices();
    }

    /// <summary>
    /// Get a specific device by ID
    /// </summary>
    public DiscoveredDevice? GetDevice(string deviceId)
    {
        return _deviceRegistry.GetDevice(deviceId);
    }

    /// <summary>
    /// Manually trigger a discovery scan
    /// </summary>
    public async Task ScanAsync(CancellationToken cancellationToken = default)
    {
        _logger.Information("Manual discovery scan triggered");

        // For mDNS, we can re-query
        if (IsRunning)
        {
            _mdnsBrowser.Stop();
            await Task.Delay(500, cancellationToken); // Brief pause
            _mdnsBrowser.Start();
        }

        _logger.Information("Manual scan completed");
    }

    /// <summary>
    /// Handle BLE device discovered
    /// </summary>
    private void OnBleDeviceDiscovered(object? sender, BleDeviceDiscovered e)
    {
        try
        {
            _logger.Debug("BLE device discovered: {DeviceId}", e.DeviceId);

            // Create or update device entry
            var device = new DiscoveredDevice
            {
                DeviceId = e.DeviceId,
                DeviceName = e.DeviceName,
                DeviceType = DeviceType.Unknown, // Will be determined by mDNS
                DiscoveredAt = e.Timestamp,
                LastSeenAt = e.Timestamp
            };

            // Add metadata
            device.Metadata["bleSignalStrength"] = e.SignalStrength.ToString();
            device.Metadata["bleAddress"] = e.BluetoothAddress.ToString("X");

            _deviceRegistry.AddOrUpdateDevice(device);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling BLE device discovery");
        }
    }

    /// <summary>
    /// Handle mDNS service discovered
    /// </summary>
    private void OnMdnsServiceDiscovered(object? sender, MdnsServiceDiscovered e)
    {
        try
        {
            _logger.Debug("mDNS service discovered: {DeviceName} at {Address}:{Port}",
                e.DeviceName, e.Addresses.FirstOrDefault(), e.Port);

            // Use service name as device ID (more reliable than BLE address)
            var deviceId = e.ServiceName;

            // Determine device type from metadata
            var deviceType = DeviceType.Unknown;
            if (e.Metadata.TryGetValue(Constants.MdnsKeys.DeviceType, out var typeStr))
            {
                Enum.TryParse<DeviceType>(typeStr, out deviceType);
            }

            var device = new DiscoveredDevice
            {
                DeviceId = deviceId,
                DeviceName = e.DeviceName,
                DeviceType = deviceType,
                IpAddress = e.Addresses.FirstOrDefault()?.ToString(),
                Port = e.Port,
                DiscoveredAt = e.Timestamp,
                LastSeenAt = e.Timestamp,
                Metadata = new Dictionary<string, string>(e.Metadata)
            };

            // Extract identity record if available
            if (e.Metadata.TryGetValue("id", out var identityHash))
            {
                device.IdentityRecord = identityHash;
            }

            _deviceRegistry.AddOrUpdateDevice(device);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling mDNS service discovery");
        }
    }

    /// <summary>
    /// Handle mDNS service removed
    /// </summary>
    private void OnMdnsServiceRemoved(object? sender, string serviceName)
    {
        try
        {
            _logger.Debug("mDNS service removed: {ServiceName}", serviceName);
            _deviceRegistry.RemoveDevice(serviceName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling mDNS service removal");
        }
    }

    /// <summary>
    /// Forward device registry events
    /// </summary>
    private void OnDeviceAdded(object? sender, DiscoveredDevice e)
    {
        DeviceDiscovered?.Invoke(this, e);
    }

    private void OnDeviceUpdated(object? sender, DiscoveredDevice e)
    {
        DeviceUpdated?.Invoke(this, e);
    }

    private void OnDeviceRemoved(object? sender, DiscoveredDevice e)
    {
        DeviceLost?.Invoke(this, e);
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();

        _bleScanner?.Dispose();
        _blePublisher?.Dispose();
        _mdnsBrowser?.Dispose();
        _mdnsPublisher?.Dispose();

        if (_deviceRegistry != null)
        {
            _deviceRegistry.DeviceAdded -= OnDeviceAdded;
            _deviceRegistry.DeviceUpdated -= OnDeviceUpdated;
            _deviceRegistry.DeviceRemoved -= OnDeviceRemoved;
            _deviceRegistry.Dispose();
        }
    }
}
