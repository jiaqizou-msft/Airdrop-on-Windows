using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using AirDropWindows.Core.Models;
using AirDropWindows.Core.Configuration;
using Serilog;

namespace AirDropWindows.Discovery.BLE;

/// <summary>
/// Scans for AirDrop BLE advertisements
/// </summary>
public class BleAdvertisementScanner : IDisposable
{
    private readonly ILogger _logger;
    private readonly BluetoothLEAdvertisementWatcher _watcher;
    private bool _isRunning;

    public event EventHandler<BleDeviceDiscovered>? DeviceDiscovered;

    public BleAdvertisementScanner(ILogger logger)
    {
        _logger = logger;

        _watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active,
            SignalStrengthFilter = new BluetoothSignalStrengthFilter
            {
                InRangeThresholdInDBm = -70, // Reasonable signal strength
                OutOfRangeThresholdInDBm = -85,
                OutOfRangeTimeout = TimeSpan.FromSeconds(10)
            }
        };

        // Register event handlers
        _watcher.Received += OnAdvertisementReceived;
        _watcher.Stopped += OnWatcherStopped;
    }

    /// <summary>
    /// Start scanning for BLE advertisements
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            _logger.Warning("BLE scanner is already running");
            return;
        }

        try
        {
            _logger.Information("Starting BLE advertisement scanner");
            _watcher.Start();
            _isRunning = true;
            _logger.Information("BLE advertisement scanner started");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start BLE advertisement scanner");
            throw;
        }
    }

    /// <summary>
    /// Stop scanning for BLE advertisements
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        try
        {
            _logger.Information("Stopping BLE advertisement scanner");
            _watcher.Stop();
            _isRunning = false;
            _logger.Information("BLE advertisement scanner stopped");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error stopping BLE advertisement scanner");
        }
    }

    /// <summary>
    /// Handle received BLE advertisements
    /// </summary>
    private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender,
        BluetoothLEAdvertisementReceivedEventArgs args)
    {
        try
        {
            // Check if this is an AirDrop advertisement
            var isAirDrop = IsAirDropAdvertisement(args.Advertisement);
            if (!isAirDrop)
            {
                return;
            }

            _logger.Debug("Received AirDrop BLE advertisement from {Address}, RSSI: {RSSI}",
                args.BluetoothAddress, args.RawSignalStrengthInDBm);

            // Parse device information
            var deviceInfo = ParseAdvertisement(args);
            if (deviceInfo != null)
            {
                DeviceDiscovered?.Invoke(this, deviceInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing BLE advertisement");
        }
    }

    /// <summary>
    /// Check if advertisement is from AirDrop device
    /// </summary>
    private bool IsAirDropAdvertisement(BluetoothLEAdvertisement advertisement)
    {
        // Look for AirDrop service UUID in advertisement
        var airdropUuid = Guid.Parse(Constants.AirDrop.BleServiceUuid);

        foreach (var serviceUuid in advertisement.ServiceUuids)
        {
            if (serviceUuid == airdropUuid)
            {
                return true;
            }
        }

        // Also check manufacturer data for Apple company ID (0x004C)
        foreach (var data in advertisement.ManufacturerData)
        {
            if (data.CompanyId == 0x004C) // Apple Inc.
            {
                // Check if it's AirDrop-related data
                // Apple uses different manufacturer data formats
                // This is a simplified check
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Parse BLE advertisement into device information
    /// </summary>
    private BleDeviceDiscovered? ParseAdvertisement(BluetoothLEAdvertisementReceivedEventArgs args)
    {
        try
        {
            var address = args.BluetoothAddress;
            var deviceId = FormatBluetoothAddress(address);

            // Extract device name from advertisement
            var deviceName = args.Advertisement.LocalName;
            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = $"Device-{deviceId[^6..]}"; // Use last 6 chars of address
            }

            return new BleDeviceDiscovered
            {
                DeviceId = deviceId,
                DeviceName = deviceName,
                BluetoothAddress = address,
                SignalStrength = args.RawSignalStrengthInDBm,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error parsing BLE advertisement");
            return null;
        }
    }

    /// <summary>
    /// Format Bluetooth address as hex string
    /// </summary>
    private static string FormatBluetoothAddress(ulong address)
    {
        var bytes = BitConverter.GetBytes(address);
        Array.Reverse(bytes);
        return BitConverter.ToString(bytes, 2).Replace("-", ":");
    }

    /// <summary>
    /// Handle watcher stopped event
    /// </summary>
    private void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender,
        BluetoothLEAdvertisementWatcherStoppedEventArgs args)
    {
        _isRunning = false;

        if (args.Error != BluetoothError.Success)
        {
            _logger.Error("BLE watcher stopped with error: {Error}", args.Error);
        }
        else
        {
            _logger.Information("BLE watcher stopped normally");
        }
    }

    public void Dispose()
    {
        Stop();
        _watcher.Received -= OnAdvertisementReceived;
        _watcher.Stopped -= OnWatcherStopped;
    }
}

/// <summary>
/// Event args for BLE device discovered
/// </summary>
public class BleDeviceDiscovered
{
    public required string DeviceId { get; init; }
    public required string DeviceName { get; init; }
    public required ulong BluetoothAddress { get; init; }
    public required short SignalStrength { get; init; }
    public required DateTime Timestamp { get; init; }
}
