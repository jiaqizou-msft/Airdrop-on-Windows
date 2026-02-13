using Makaretu.Dns;
using AirDropWindows.Core.Models;
using AirDropWindows.Core.Configuration;
using Serilog;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AirDropWindows.Discovery.mDNS;

/// <summary>
/// Publishes AirDrop service using mDNS/Bonjour
/// </summary>
public class MdnsServicePublisher : IDisposable
{
    private readonly ILogger _logger;
    private readonly ServiceDiscovery _serviceDiscovery;
    private readonly DeviceIdentity _deviceIdentity;
    private readonly int _port;
    private ServiceProfile? _serviceProfile;
    private bool _isRunning;

    public MdnsServicePublisher(ILogger logger, DeviceIdentity deviceIdentity, int port)
    {
        _logger = logger;
        _deviceIdentity = deviceIdentity;
        _port = port;
        _serviceDiscovery = new ServiceDiscovery();
    }

    /// <summary>
    /// Start advertising AirDrop service
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            _logger.Warning("mDNS publisher is already running");
            return;
        }

        try
        {
            _logger.Information("Starting mDNS service publisher for device: {DeviceName}",
                _deviceIdentity.DeviceName);

            // Create service profile
            _serviceProfile = CreateServiceProfile();

            // Advertise service
            _serviceDiscovery.Advertise(_serviceProfile);

            _isRunning = true;
            _logger.Information("mDNS service publisher started: {ServiceName}",
                _serviceProfile.FullyQualifiedName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start mDNS service publisher");
            throw;
        }
    }

    /// <summary>
    /// Stop advertising service
    /// </summary>
    public void Stop()
    {
        if (!_isRunning || _serviceProfile == null)
        {
            return;
        }

        try
        {
            _logger.Information("Stopping mDNS service publisher");

            // Unadvertise service
            _serviceDiscovery.Unadvertise(_serviceProfile);

            _isRunning = false;
            _logger.Information("mDNS service publisher stopped");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error stopping mDNS service publisher");
        }
    }

    /// <summary>
    /// Create service profile with device information
    /// </summary>
    private ServiceProfile CreateServiceProfile()
    {
        // Service instance name: "DeviceName._airdrop._tcp.local"
        var serviceName = SanitizeServiceName(_deviceIdentity.DeviceName);
        var hostName = $"{Environment.MachineName}.local";

        var profile = new ServiceProfile(
            serviceName,
            Constants.AirDrop.ServiceType,
            (ushort)_port,
            new[] { GetLocalIPAddress() }
        )
        {
            HostName = hostName
        };

        // Add TXT record metadata
        profile.Resources.Add(CreateTxtRecord(profile.FullyQualifiedName));

        _logger.Debug("Created service profile: {ServiceName} on {HostName}:{Port}",
            profile.FullyQualifiedName, hostName, _port);

        return profile;
    }

    /// <summary>
    /// Create TXT record with device metadata
    /// </summary>
    private TXTRecord CreateTxtRecord(DomainName serviceName)
    {
        var txtRecord = new TXTRecord
        {
            Name = serviceName
        };

        // Add metadata key-value pairs
        var metadata = new Dictionary<string, string>
        {
            [Constants.MdnsKeys.DeviceType] = _deviceIdentity.DeviceType.ToString(),
            [Constants.MdnsKeys.Transport] = "wifidirect,wifi", // Supported transports
            [Constants.MdnsKeys.Capabilities] = "send,receive", // Capabilities
            [Constants.MdnsKeys.Version] = Constants.Version
        };

        // Add identity record if available
        if (!string.IsNullOrEmpty(_deviceIdentity.IdentityRecord))
        {
            // Truncate to reasonable length for TXT record
            var identityHash = _deviceIdentity.IdentityRecord[..Math.Min(32, _deviceIdentity.IdentityRecord.Length)];
            metadata["id"] = identityHash;
        }

        // Format as "key=value" strings
        foreach (var kvp in metadata)
        {
            txtRecord.Strings.Add($"{kvp.Key}={kvp.Value}");
        }

        return txtRecord;
    }

    /// <summary>
    /// Get local IP address
    /// </summary>
    private IPAddress GetLocalIPAddress()
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
                ni.Name.Contains("Wireless", StringComparison.OrdinalIgnoreCase));

            var selectedInterface = wifiInterface ?? interfaces.FirstOrDefault();

            if (selectedInterface != null)
            {
                var ipProperties = selectedInterface.GetIPProperties();
                var ipv4Address = ipProperties.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                if (ipv4Address != null)
                {
                    _logger.Debug("Using local IP address: {IpAddress} from interface: {Interface}",
                        ipv4Address.Address, selectedInterface.Name);
                    return ipv4Address.Address;
                }
            }

            // Fallback to any address
            _logger.Warning("No suitable network interface found, using fallback IP");
            return IPAddress.Any;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting local IP address");
            return IPAddress.Any;
        }
    }

    /// <summary>
    /// Sanitize service name for mDNS
    /// </summary>
    private static string SanitizeServiceName(string name)
    {
        // Remove invalid characters for mDNS service names
        var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());

        // Limit length
        if (sanitized.Length > 63)
        {
            sanitized = sanitized[..63];
        }

        return string.IsNullOrEmpty(sanitized) ? "Windows-Device" : sanitized;
    }

    /// <summary>
    /// Update service advertisement with new device information
    /// </summary>
    public void UpdateService(DeviceIdentity newIdentity)
    {
        _logger.Information("Updating mDNS service advertisement");

        var wasRunning = _isRunning;

        if (wasRunning)
        {
            Stop();
        }

        // Service would need to be recreated with new identity
        // For now, just restart if it was running

        if (wasRunning)
        {
            Start();
        }
    }

    public void Dispose()
    {
        Stop();
        _serviceDiscovery.Dispose();
    }
}
