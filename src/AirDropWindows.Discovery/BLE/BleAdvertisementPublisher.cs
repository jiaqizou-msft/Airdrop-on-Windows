using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using AirDropWindows.Core.Models;
using AirDropWindows.Core.Configuration;
using Serilog;

namespace AirDropWindows.Discovery.BLE;

/// <summary>
/// Publishes BLE advertisements to make this device discoverable
/// </summary>
public class BleAdvertisementPublisher : IDisposable
{
    private readonly ILogger _logger;
    private readonly BluetoothLEAdvertisementPublisher _publisher;
    private readonly DeviceIdentity _deviceIdentity;
    private bool _isRunning;

    public BleAdvertisementPublisher(ILogger logger, DeviceIdentity deviceIdentity)
    {
        _logger = logger;
        _deviceIdentity = deviceIdentity;

        // Create advertisement
        var advertisement = CreateAdvertisement();

        _publisher = new BluetoothLEAdvertisementPublisher(advertisement);
        _publisher.StatusChanged += OnStatusChanged;
    }

    /// <summary>
    /// Create BLE advertisement with AirDrop service UUID and device info
    /// </summary>
    private BluetoothLEAdvertisement CreateAdvertisement()
    {
        var advertisement = new BluetoothLEAdvertisement
        {
            LocalName = _deviceIdentity.DeviceName
        };

        // Add AirDrop service UUID
        var airdropUuid = Guid.Parse(Constants.AirDrop.BleServiceUuid);
        advertisement.ServiceUuids.Add(airdropUuid);

        // Add manufacturer-specific data (Apple company ID: 0x004C)
        var manufacturerData = CreateManufacturerData();
        if (manufacturerData != null)
        {
            advertisement.ManufacturerData.Add(manufacturerData);
        }

        // Set flags for discoverability
        advertisement.Flags = BluetoothLEAdvertisementFlags.GeneralDiscoverableMode |
                             BluetoothLEAdvertisementFlags.ClassicNotSupported;

        return advertisement;
    }

    /// <summary>
    /// Create manufacturer-specific data payload
    /// </summary>
    private BluetoothLEManufacturerData? CreateManufacturerData()
    {
        try
        {
            // Apple company ID
            const ushort appleCompanyId = 0x004C;

            // Create data payload
            // Format: [Type][Length][Data...]
            // This is simplified - real AirDrop uses proprietary format
            using var dataWriter = new DataWriter();

            // Type: AirDrop (0x05 - unofficial)
            dataWriter.WriteByte(0x05);

            // Device flags (simplified)
            dataWriter.WriteByte(0x01); // Available for receiving

            // Identity hash (first 8 bytes)
            if (!string.IsNullOrEmpty(_deviceIdentity.IdentityRecord))
            {
                var identityBytes = Convert.FromHexString(_deviceIdentity.IdentityRecord[..16]);
                dataWriter.WriteBytes(identityBytes);
            }

            var buffer = dataWriter.DetachBuffer();

            return new BluetoothLEManufacturerData(appleCompanyId, buffer);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create manufacturer data");
            return null;
        }
    }

    /// <summary>
    /// Start advertising
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            _logger.Warning("BLE publisher is already running");
            return;
        }

        try
        {
            _logger.Information("Starting BLE advertisement publisher for device: {DeviceName}",
                _deviceIdentity.DeviceName);

            _publisher.Start();
            _isRunning = true;

            _logger.Information("BLE advertisement publisher started");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start BLE advertisement publisher");
            throw;
        }
    }

    /// <summary>
    /// Stop advertising
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        try
        {
            _logger.Information("Stopping BLE advertisement publisher");
            _publisher.Stop();
            _isRunning = false;
            _logger.Information("BLE advertisement publisher stopped");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error stopping BLE advertisement publisher");
        }
    }

    /// <summary>
    /// Handle publisher status changes
    /// </summary>
    private void OnStatusChanged(BluetoothLEAdvertisementPublisher sender,
        BluetoothLEAdvertisementPublisherStatusChangedEventArgs args)
    {
        _logger.Information("BLE publisher status changed: {Status}, Error: {Error}",
            args.Status, args.Error);

        switch (args.Status)
        {
            case BluetoothLEAdvertisementPublisherStatus.Started:
                _isRunning = true;
                break;

            case BluetoothLEAdvertisementPublisherStatus.Stopped:
            case BluetoothLEAdvertisementPublisherStatus.Aborted:
                _isRunning = false;
                break;

            case BluetoothLEAdvertisementPublisherStatus.Waiting:
                // Waiting for resources
                _logger.Warning("BLE publisher waiting for resources");
                break;
        }

        if (args.Error != BluetoothError.Success)
        {
            _logger.Error("BLE publisher error: {Error}", args.Error);
        }
    }

    /// <summary>
    /// Update advertisement with new device identity
    /// </summary>
    public void UpdateAdvertisement(DeviceIdentity newIdentity)
    {
        var wasRunning = _isRunning;

        if (wasRunning)
        {
            Stop();
        }

        // Advertisement is immutable, so we'd need to recreate the publisher
        // For now, just log the update request
        _logger.Information("Advertisement update requested (requires restart)");

        if (wasRunning)
        {
            Start();
        }
    }

    public void Dispose()
    {
        Stop();
        _publisher.StatusChanged -= OnStatusChanged;
    }
}
