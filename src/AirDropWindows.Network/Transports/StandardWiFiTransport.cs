using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Models;
using AirDropWindows.Core.Configuration;
using Serilog;

namespace AirDropWindows.Network.Transports;

/// <summary>
/// Standard Wi-Fi transport for same-network connections
/// </summary>
public class StandardWiFiTransport : INetworkTransport
{
    private readonly ILogger _logger;
    private readonly AppSettings _settings;
    private TcpListener? _listener;
    private bool _isListening;

    public string TransportType => "StandardWiFi";

    public StandardWiFiTransport(ILogger logger, AppSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    /// <summary>
    /// Check if standard Wi-Fi is available
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // Check if we have any active network interfaces
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();

            var isAvailable = interfaces.Count > 0;
            _logger.Information("Standard Wi-Fi availability: {IsAvailable}", isAvailable);

            await Task.CompletedTask;
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking standard Wi-Fi availability");
            return false;
        }
    }

    /// <summary>
    /// Connect to a device on the same network
    /// </summary>
    public async Task<NetworkConnection> ConnectAsync(
        DiscoveredDevice device,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(device.IpAddress))
            {
                throw new InvalidOperationException("Device IP address is not available");
            }

            _logger.Information("Connecting to {DeviceId} at {IpAddress}:{Port} via standard Wi-Fi",
                device.DeviceId, device.IpAddress, device.Port);

            var client = new TcpClient();
            await client.ConnectAsync(device.IpAddress, device.Port, cancellationToken);

            var localEndpoint = (IPEndPoint)client.Client.LocalEndPoint!;
            var remoteEndpoint = (IPEndPoint)client.Client.RemoteEndPoint!;

            _logger.Information("Connected to {DeviceId}", device.DeviceId);

            var connection = new NetworkConnection
            {
                ConnectionId = Guid.NewGuid().ToString(),
                LocalAddress = localEndpoint.Address.ToString(),
                RemoteAddress = remoteEndpoint.Address.ToString(),
                LocalPort = localEndpoint.Port,
                RemotePort = remoteEndpoint.Port,
                TransportType = TransportType,
                NativeConnection = client
            };

            return connection;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to connect via standard Wi-Fi to {DeviceId}", device.DeviceId);
            throw;
        }
    }

    /// <summary>
    /// Listen for incoming TCP connections
    /// </summary>
    public async Task<NetworkConnection> ListenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_isListening)
            {
                throw new InvalidOperationException("Already listening for connections");
            }

            var localIp = await GetLocalIpAddressAsync();
            if (localIp == null)
            {
                throw new InvalidOperationException("Cannot determine local IP address");
            }

            _logger.Information("Starting standard Wi-Fi listener on {IpAddress}:{Port}",
                localIp, _settings.Network.Port);

            _listener = new TcpListener(IPAddress.Parse(localIp), _settings.Network.Port);
            _listener.Start();
            _isListening = true;

            _logger.Information("Listening for connections on {IpAddress}:{Port}",
                localIp, _settings.Network.Port);

            // Accept incoming connection
            var client = await _listener.AcceptTcpClientAsync(cancellationToken);

            var localEndpoint = (IPEndPoint)client.Client.LocalEndPoint!;
            var remoteEndpoint = (IPEndPoint)client.Client.RemoteEndPoint!;

            _logger.Information("Accepted connection from {RemoteAddress}:{RemotePort}",
                remoteEndpoint.Address, remoteEndpoint.Port);

            var connection = new NetworkConnection
            {
                ConnectionId = Guid.NewGuid().ToString(),
                LocalAddress = localEndpoint.Address.ToString(),
                RemoteAddress = remoteEndpoint.Address.ToString(),
                LocalPort = localEndpoint.Port,
                RemotePort = remoteEndpoint.Port,
                TransportType = TransportType,
                NativeConnection = client
            };

            return connection;
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Standard Wi-Fi listener cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in standard Wi-Fi listener");
            throw;
        }
    }

    /// <summary>
    /// Disconnect from a device
    /// </summary>
    public async Task DisconnectAsync(NetworkConnection connection)
    {
        try
        {
            _logger.Information("Disconnecting standard Wi-Fi connection: {ConnectionId}",
                connection.ConnectionId);

            if (connection.NativeConnection is TcpClient client)
            {
                client.Close();
                client.Dispose();
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disconnecting standard Wi-Fi connection");
        }
    }

    /// <summary>
    /// Get local IP address
    /// </summary>
    public async Task<string?> GetLocalIpAddressAsync()
    {
        try
        {
            // Get all network interfaces
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();

            // Prefer Wi-Fi adapter
            var wifiInterface = interfaces.FirstOrDefault(ni =>
                ni.Name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                ni.Name.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                ni.Name.Contains("WLAN", StringComparison.OrdinalIgnoreCase));

            var selectedInterface = wifiInterface ?? interfaces.FirstOrDefault();

            if (selectedInterface != null)
            {
                var ipProperties = selectedInterface.GetIPProperties();
                var ipv4Address = ipProperties.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork &&
                                        !IPAddress.IsLoopback(a.Address));

                if (ipv4Address != null)
                {
                    var address = ipv4Address.Address.ToString();
                    _logger.Debug("Local IP address: {IpAddress} from interface: {Interface}",
                        address, selectedInterface.Name);

                    await Task.CompletedTask;
                    return address;
                }
            }

            _logger.Warning("No suitable network interface found");
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting local IP address");
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

    /// <summary>
    /// Stop listening for connections
    /// </summary>
    public void StopListening()
    {
        if (_isListening && _listener != null)
        {
            _logger.Information("Stopping standard Wi-Fi listener");
            _listener.Stop();
            _isListening = false;
        }
    }

    public void Dispose()
    {
        StopListening();
    }
}
