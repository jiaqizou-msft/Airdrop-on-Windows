namespace AirDropWindows.Core.Models;

/// <summary>
/// Represents the identity of this device for AirDrop
/// </summary>
public class DeviceIdentity
{
    /// <summary>
    /// Unique device identifier (persistent)
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Device name shown to other devices
    /// </summary>
    public required string DeviceName { get; set; }

    /// <summary>
    /// Device type
    /// </summary>
    public DeviceType DeviceType { get; init; } = DeviceType.WindowsPC;

    /// <summary>
    /// User's email address (for identity hash)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User's phone number (for identity hash)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Computed identity record data (SHA256 hash)
    /// </summary>
    public string? IdentityRecord { get; set; }

    /// <summary>
    /// Discovery mode setting
    /// </summary>
    public DiscoveryMode DiscoveryMode { get; set; } = DiscoveryMode.Everyone;

    /// <summary>
    /// Whether to accept transfers automatically
    /// </summary>
    public bool AutoAccept { get; set; }

    /// <summary>
    /// Default save location for received files
    /// </summary>
    public string? DefaultSaveLocation { get; set; }

    /// <summary>
    /// Self-signed certificate thumbprint
    /// </summary>
    public string? CertificateThumbprint { get; set; }

    /// <summary>
    /// Certificate creation date
    /// </summary>
    public DateTime? CertificateCreatedAt { get; set; }

    /// <summary>
    /// Certificate expiration date
    /// </summary>
    public DateTime? CertificateExpiresAt { get; set; }
}

/// <summary>
/// Discovery mode for AirDrop visibility
/// </summary>
public enum DiscoveryMode
{
    /// <summary>
    /// Not discoverable by any devices
    /// </summary>
    Off,

    /// <summary>
    /// Discoverable only by contacts (simplified - not fully implemented)
    /// </summary>
    ContactsOnly,

    /// <summary>
    /// Discoverable by all nearby devices
    /// </summary>
    Everyone
}
