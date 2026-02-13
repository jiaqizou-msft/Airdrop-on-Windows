namespace AirDropWindows.Core.Models;

/// <summary>
/// Represents a discovered AirDrop-compatible device
/// </summary>
public class DiscoveredDevice
{
    /// <summary>
    /// Unique device identifier
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Human-readable device name (e.g., "John's iPhone")
    /// </summary>
    public required string DeviceName { get; init; }

    /// <summary>
    /// Device type (iPhone, iPad, Mac, Windows PC)
    /// </summary>
    public DeviceType DeviceType { get; init; }

    /// <summary>
    /// IP address of the device
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Port number for AirDrop service
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Timestamp when device was first discovered
    /// </summary>
    public DateTime DiscoveredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of last activity from this device
    /// </summary>
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the device is currently available
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Current connection state
    /// </summary>
    public ConnectionState ConnectionState { get; set; } = ConnectionState.Disconnected;

    /// <summary>
    /// Identity record data (hashed email/phone)
    /// </summary>
    public string? IdentityRecord { get; set; }

    /// <summary>
    /// Service discovery metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Device type enumeration
/// </summary>
public enum DeviceType
{
    Unknown,
    iPhone,
    iPad,
    Mac,
    WindowsPC
}

/// <summary>
/// Connection state enumeration
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Discovering,
    Connecting,
    Connected,
    Authenticating,
    Authenticated,
    Transferring,
    Error
}
