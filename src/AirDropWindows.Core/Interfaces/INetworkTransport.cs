using AirDropWindows.Core.Models;

namespace AirDropWindows.Core.Interfaces;

/// <summary>
/// Abstraction for network transport layer (WiFi Direct, Standard WiFi, etc.)
/// </summary>
public interface INetworkTransport
{
    /// <summary>
    /// Transport type name
    /// </summary>
    string TransportType { get; }

    /// <summary>
    /// Whether this transport is available on the current system
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Connect to a discovered device
    /// </summary>
    Task<NetworkConnection> ConnectAsync(DiscoveredDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Listen for incoming connections
    /// </summary>
    Task<NetworkConnection> ListenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from a device
    /// </summary>
    Task DisconnectAsync(NetworkConnection connection);

    /// <summary>
    /// Get the local IP address for this transport
    /// </summary>
    Task<string?> GetLocalIpAddressAsync();

    /// <summary>
    /// Get the port number for this transport
    /// </summary>
    int GetPort();
}

/// <summary>
/// Represents an active network connection
/// </summary>
public class NetworkConnection
{
    public required string ConnectionId { get; init; }
    public required string LocalAddress { get; init; }
    public required string RemoteAddress { get; init; }
    public required int LocalPort { get; init; }
    public required int RemotePort { get; init; }
    public required string TransportType { get; init; }
    public DateTime ConnectedAt { get; init; } = DateTime.UtcNow;
    public ConnectionState State { get; set; } = ConnectionState.Connected;
    public object? NativeConnection { get; set; }
}
