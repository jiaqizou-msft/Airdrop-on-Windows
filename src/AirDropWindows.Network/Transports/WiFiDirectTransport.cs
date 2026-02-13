using Windows.Devices.WiFiDirect;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Networking;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Models;
using AirDropWindows.Core.Configuration;
using Serilog;

namespace AirDropWindows.Network.Transports;

/// <summary>
/// Wi-Fi Direct transport implementation (replaces Apple's AWDL)
/// </summary>
public class WiFiDirectTransport : INetworkTransport
{
    private readonly ILogger _logger;
    private readonly AppSettings _settings;
    private WiFiDirectAdvertisementPublisher? _publisher;
    private WiFiDirectConnectionListener? _listener;
    private bool _isListening;

    public string TransportType => "WiFiDirect";

    public WiFiDirectTransport(ILogger logger, AppSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    /// <summary>
    /// Check if Wi-Fi Direct is available on this system
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // Check if Wi-Fi Direct is supported
            var deviceSelector = WiFiDirectDevice.GetDeviceSelector(
                WiFiDirectDeviceSelectorType.AssociationEndpoint);

            var devices = await DeviceInformation.FindAllAsync(deviceSelector);

            var isAvailable = devices.Count > 0;
            _logger.Information("Wi-Fi Direct availability: {IsAvailable}", isAvailable);

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking Wi-Fi Direct availability");
            return false;
        }
    }

    /// <summary>
    /// Connect to a discovered device using Wi-Fi Direct
    /// </summary>
    public async Task<NetworkConnection> ConnectAsync(
        DiscoveredDevice device,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Information("Connecting to device {DeviceId} via Wi-Fi Direct", device.DeviceId);

            // Get Wi-Fi Direct device selector
            var deviceSelector = WiFiDirectDevice.GetDeviceSelector(
                WiFiDirectDeviceSelectorType.AssociationEndpoint);

            // Find the specific device
            var deviceInfos = await DeviceInformation.FindAllAsync(deviceSelector);
            var targetDevice = deviceInfos.FirstOrDefault(d =>
                d.Name.Contains(device.DeviceName) ||
                d.Id.Contains(device.DeviceId));

            if (targetDevice == null)
            {
                throw new InvalidOperationException(
                    $"Wi-Fi Direct device not found: {device.DeviceName}");
            }

            // Connect to the device
            var wiFiDirectDevice = await WiFiDirectDevice.FromIdAsync(targetDevice.Id);
            if (wiFiDirectDevice == null)
            {
                throw new InvalidOperationException("Failed to get Wi-Fi Direct device");
            }

            // Get connection endpoint information
            var endpointPairs = wiFiDirectDevice.GetConnectionEndpointPairs();
            if (endpointPairs.Count == 0)
            {
                throw new InvalidOperationException("No connection endpoints available");
            }

            var endpointPair = endpointPairs[0];
            var remoteHostName = endpointPair.RemoteHostName;

            _logger.Information("Wi-Fi Direct connection established to {HostName}",
                remoteHostName?.DisplayName);

            var connection = new NetworkConnection
            {
                ConnectionId = Guid.NewGuid().ToString(),
                LocalAddress = endpointPair.LocalHostName?.DisplayName ?? "unknown",
                RemoteAddress = remoteHostName?.DisplayName ?? device.IpAddress ?? "unknown",
                LocalPort = _settings.Network.Port,
                RemotePort = device.Port,
                TransportType = TransportType,
                NativeConnection = wiFiDirectDevice
            };

            return connection;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to connect via Wi-Fi Direct to {DeviceId}", device.DeviceId);
            throw;
        }
    }

    /// <summary>
    /// Listen for incoming Wi-Fi Direct connections
    /// </summary>
    public async Task<NetworkConnection> ListenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_isListening)
            {
                throw new InvalidOperationException("Already listening for connections");
            }

            _logger.Information("Starting Wi-Fi Direct listener");

            // Create connection listener
            _listener = new WiFiDirectConnectionListener();

            // Configure listener
            _listener.ConnectionRequested += OnConnectionRequested;

            // Start advertising
            await StartAdvertisingAsync();

            _isListening = true;

            // Wait for incoming connection (this is simplified - real implementation
            // would use TaskCompletionSource to properly wait for connection)
            _logger.Information("Wi-Fi Direct listener started, waiting for connections");

            // For now, return a placeholder - real implementation would wait for actual connection
            await Task.Delay(Timeout.Infinite, cancellationToken);

            throw new OperationCanceledException();
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Wi-Fi Direct listener cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in Wi-Fi Direct listener");
            throw;
        }
    }

    /// <summary>
    /// Start Wi-Fi Direct advertisement
    /// </summary>
    private async Task StartAdvertisingAsync()
    {
        try
        {
            _publisher = new WiFiDirectAdvertisementPublisher();

            // Configure advertisement
            _publisher.Advertisement.IsAutonomousGroupOwnerEnabled = true;
            _publisher.Advertisement.LegacySettings.IsEnabled = false;

            // Set service name
            _publisher.Advertisement.ListenStateDiscoverability =
                WiFiDirectAdvertisementListenStateDiscoverability.Normal;

            // Start publishing
            _publisher.StatusChanged += OnAdvertisementStatusChanged;
            _publisher.Start();

            _logger.Information("Wi-Fi Direct advertisement started");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start Wi-Fi Direct advertisement");
            throw;
        }
    }

    /// <summary>
    /// Handle incoming connection request
    /// </summary>
    private void OnConnectionRequested(
        WiFiDirectConnectionListener sender,
        WiFiDirectConnectionRequestedEventArgs args)
    {
        try
        {
            var request = args.GetConnectionRequest();
            _logger.Information("Wi-Fi Direct connection requested from device: {DeviceId}",
                request.DeviceInformation.Id);

            // Accept the connection (in real implementation, this would be queued
            // and handled by the ConnectionManager)
            // For now, just log it
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling connection request");
        }
    }

    /// <summary>
    /// Handle advertisement status changes
    /// </summary>
    private void OnAdvertisementStatusChanged(
        WiFiDirectAdvertisementPublisher sender,
        WiFiDirectAdvertisementPublisherStatusChangedEventArgs args)
    {
        _logger.Information("Wi-Fi Direct advertisement status: {Status}, Error: {Error}",
            args.Status, args.Error);
    }

    /// <summary>
    /// Disconnect from a device
    /// </summary>
    public async Task DisconnectAsync(NetworkConnection connection)
    {
        try
        {
            _logger.Information("Disconnecting Wi-Fi Direct connection: {ConnectionId}",
                connection.ConnectionId);

            if (connection.NativeConnection is WiFiDirectDevice device)
            {
                device.Dispose();
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disconnecting Wi-Fi Direct connection");
        }
    }

    /// <summary>
    /// Get local IP address for Wi-Fi Direct
    /// </summary>
    public async Task<string?> GetLocalIpAddressAsync()
    {
        try
        {
            // For Wi-Fi Direct, the IP is assigned dynamically after connection
            // Return a placeholder for now
            await Task.CompletedTask;
            return "169.254.x.x"; // Link-local address range
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting Wi-Fi Direct local IP");
            return null;
        }
    }

    /// <summary>
    /// Get port number for this transport
    /// </summary>
    public int GetPort()
    {
        return _settings.Network.Port;
    }

    public void Dispose()
    {
        _isListening = false;

        if (_publisher != null)
        {
            _publisher.StatusChanged -= OnAdvertisementStatusChanged;
            _publisher.Stop();
            _publisher = null;
        }

        if (_listener != null)
        {
            _listener.ConnectionRequested -= OnConnectionRequested;
            _listener = null;
        }
    }
}
