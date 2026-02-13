using System.Collections.Concurrent;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Models;
using AirDropWindows.Core.Configuration;
using AirDropWindows.Network.Transports;
using Serilog;

namespace AirDropWindows.Network;

/// <summary>
/// Manages network connections and transport selection
/// </summary>
public class ConnectionManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly AppSettings _settings;
    private readonly List<INetworkTransport> _transports;
    private readonly ConcurrentDictionary<string, NetworkConnection> _activeConnections;

    public ConnectionManager(ILogger logger, AppSettings settings)
    {
        _logger = logger;
        _settings = settings;
        _activeConnections = new ConcurrentDictionary<string, NetworkConnection>();

        // Initialize available transports
        _transports = new List<INetworkTransport>();

        if (settings.Network.UseWiFiDirect)
        {
            _transports.Add(new WiFiDirectTransport(logger, settings));
        }

        if (settings.Network.UseStandardWiFi)
        {
            _transports.Add(new StandardWiFiTransport(logger, settings));
        }
    }

    /// <summary>
    /// Connect to a device using the best available transport
    /// </summary>
    public async Task<NetworkConnection> ConnectAsync(
        DiscoveredDevice device,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Attempting to connect to device: {DeviceId}", device.DeviceId);

        Exception? lastException = null;

        // Try each transport in priority order
        foreach (var transport in _transports)
        {
            try
            {
                var isAvailable = await transport.IsAvailableAsync();
                if (!isAvailable)
                {
                    _logger.Debug("Transport {TransportType} is not available, skipping",
                        transport.TransportType);
                    continue;
                }

                _logger.Information("Attempting connection via {TransportType}",
                    transport.TransportType);

                var connection = await transport.ConnectAsync(device, cancellationToken);

                // Add to active connections
                _activeConnections[connection.ConnectionId] = connection;

                _logger.Information("Successfully connected to {DeviceId} via {TransportType}",
                    device.DeviceId, transport.TransportType);

                return connection;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to connect via {TransportType}", transport.TransportType);
                lastException = ex;
                // Continue to next transport
            }
        }

        // All transports failed
        var message = $"Failed to connect to device {device.DeviceId} using any available transport";
        _logger.Error(lastException, message);
        throw new InvalidOperationException(message, lastException);
    }

    /// <summary>
    /// Disconnect from a device
    /// </summary>
    public async Task DisconnectAsync(string connectionId)
    {
        if (_activeConnections.TryRemove(connectionId, out var connection))
        {
            _logger.Information("Disconnecting connection: {ConnectionId}", connectionId);

            // Find the transport that created this connection
            var transport = _transports.FirstOrDefault(t =>
                t.TransportType == connection.TransportType);

            if (transport != null)
            {
                await transport.DisconnectAsync(connection);
            }

            connection.State = ConnectionState.Disconnected;
        }
    }

    /// <summary>
    /// Get an active connection by ID
    /// </summary>
    public NetworkConnection? GetConnection(string connectionId)
    {
        return _activeConnections.TryGetValue(connectionId, out var connection)
            ? connection
            : null;
    }

    /// <summary>
    /// Get all active connections
    /// </summary>
    public IReadOnlyCollection<NetworkConnection> GetActiveConnections()
    {
        return _activeConnections.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Start listening for incoming connections on all transports
    /// </summary>
    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting connection listeners");

        var tasks = _transports.Select(async transport =>
        {
            try
            {
                var isAvailable = await transport.IsAvailableAsync();
                if (!isAvailable)
                {
                    _logger.Debug("Transport {TransportType} not available for listening",
                        transport.TransportType);
                    return;
                }

                _logger.Information("Starting listener for {TransportType}",
                    transport.TransportType);

                // Start listening (this would run continuously in background)
                // For now, just log that we would start listening
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start listener for {TransportType}",
                    transport.TransportType);
            }
        });

        await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        _logger.Information("Disposing ConnectionManager");

        foreach (var connection in _activeConnections.Values)
        {
            try
            {
                DisconnectAsync(connection.ConnectionId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error disconnecting {ConnectionId}", connection.ConnectionId);
            }
        }

        foreach (var transport in _transports)
        {
            if (transport is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
