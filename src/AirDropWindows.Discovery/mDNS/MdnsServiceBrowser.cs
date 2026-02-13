using Makaretu.Dns;
using AirDropWindows.Core.Models;
using AirDropWindows.Core.Configuration;
using Serilog;
using System.Net;

namespace AirDropWindows.Discovery.mDNS;

/// <summary>
/// Browses for AirDrop services using mDNS/Bonjour
/// </summary>
public class MdnsServiceBrowser : IDisposable
{
    private readonly ILogger _logger;
    private readonly ServiceDiscovery _serviceDiscovery;
    private bool _isRunning;

    public event EventHandler<MdnsServiceDiscovered>? ServiceDiscovered;
    public event EventHandler<string>? ServiceRemoved;

    public MdnsServiceBrowser(ILogger logger)
    {
        _logger = logger;
        _serviceDiscovery = new ServiceDiscovery();

        // Register event handlers
        _serviceDiscovery.ServiceDiscovered += OnServiceDiscovered;
        _serviceDiscovery.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown += OnServiceInstanceShutdown;
    }

    /// <summary>
    /// Start browsing for AirDrop services
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            _logger.Warning("mDNS browser is already running");
            return;
        }

        try
        {
            _logger.Information("Starting mDNS service browser for {ServiceType}",
                Constants.AirDrop.ServiceType);

            // Query for AirDrop services
            _serviceDiscovery.QueryServiceInstances(Constants.AirDrop.ServiceType);

            _isRunning = true;
            _logger.Information("mDNS service browser started");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start mDNS service browser");
            throw;
        }
    }

    /// <summary>
    /// Stop browsing for services
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        try
        {
            _logger.Information("Stopping mDNS service browser");
            _isRunning = false;
            _logger.Information("mDNS service browser stopped");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error stopping mDNS service browser");
        }
    }

    /// <summary>
    /// Handle service type discovered
    /// </summary>
    private void OnServiceDiscovered(object? sender, DomainName serviceName)
    {
        _logger.Debug("Service discovered: {ServiceName}", serviceName);
    }

    /// <summary>
    /// Handle service instance discovered
    /// </summary>
    private void OnServiceInstanceDiscovered(object? sender, ServiceInstanceDiscoveryEventArgs e)
    {
        try
        {
            var message = e.Message;
            var serviceName = e.ServiceInstanceName;

            _logger.Information("Service instance discovered: {ServiceName}", serviceName);

            // Extract service information
            var serviceInfo = ParseServiceInstance(message, serviceName);
            if (serviceInfo != null)
            {
                ServiceDiscovered?.Invoke(this, serviceInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing discovered service instance");
        }
    }

    /// <summary>
    /// Handle service instance shutdown
    /// </summary>
    private void OnServiceInstanceShutdown(object? sender, ServiceInstanceShutdownEventArgs e)
    {
        try
        {
            var serviceName = e.ServiceInstanceName.ToString();
            _logger.Information("Service instance removed: {ServiceName}", serviceName);

            ServiceRemoved?.Invoke(this, serviceName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing service shutdown");
        }
    }

    /// <summary>
    /// Parse service instance information from DNS message
    /// </summary>
    private MdnsServiceDiscovered? ParseServiceInstance(Message message, DomainName serviceName)
    {
        try
        {
            // Find SRV record for port and target
            var srvRecord = message.Answers
                .OfType<SRVRecord>()
                .FirstOrDefault(r => r.Name == serviceName);

            if (srvRecord == null)
            {
                _logger.Debug("No SRV record found for {ServiceName}", serviceName);
                return null;
            }

            // Find A/AAAA records for IP addresses
            var addresses = new List<IPAddress>();

            var aRecords = message.AdditionalRecords
                .OfType<ARecord>()
                .Where(r => r.Name == srvRecord.Target);

            addresses.AddRange(aRecords.Select(r => r.Address));

            var aaaaRecords = message.AdditionalRecords
                .OfType<AAAARecord>()
                .Where(r => r.Name == srvRecord.Target);

            addresses.AddRange(aaaaRecords.Select(r => r.Address));

            if (addresses.Count == 0)
            {
                _logger.Debug("No IP addresses found for {ServiceName}", serviceName);
                return null;
            }

            // Find TXT record for metadata
            var txtRecord = message.AdditionalRecords
                .OfType<TXTRecord>()
                .FirstOrDefault(r => r.Name == serviceName);

            var metadata = ParseTxtRecord(txtRecord);

            // Extract device name from service instance name
            // Format: "DeviceName._airdrop._tcp.local"
            var deviceName = serviceName.Labels.FirstOrDefault() ?? "Unknown Device";

            var serviceInfo = new MdnsServiceDiscovered
            {
                ServiceName = serviceName.ToString(),
                DeviceName = deviceName,
                HostName = srvRecord.Target.ToString(),
                Port = srvRecord.Port,
                Addresses = addresses,
                Metadata = metadata,
                Timestamp = DateTime.UtcNow
            };

            _logger.Debug("Parsed service: {DeviceName} at {Address}:{Port}",
                deviceName, addresses.FirstOrDefault(), srvRecord.Port);

            return serviceInfo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error parsing service instance");
            return null;
        }
    }

    /// <summary>
    /// Parse TXT record into metadata dictionary
    /// </summary>
    private Dictionary<string, string> ParseTxtRecord(TXTRecord? txtRecord)
    {
        var metadata = new Dictionary<string, string>();

        if (txtRecord == null)
        {
            return metadata;
        }

        foreach (var text in txtRecord.Strings)
        {
            var parts = text.Split('=', 2);
            if (parts.Length == 2)
            {
                metadata[parts[0]] = parts[1];
            }
        }

        return metadata;
    }

    public void Dispose()
    {
        Stop();
        _serviceDiscovery.ServiceDiscovered -= OnServiceDiscovered;
        _serviceDiscovery.ServiceInstanceDiscovered -= OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown -= OnServiceInstanceShutdown;
        _serviceDiscovery.Dispose();
    }
}

/// <summary>
/// Event args for mDNS service discovered
/// </summary>
public class MdnsServiceDiscovered
{
    public required string ServiceName { get; init; }
    public required string DeviceName { get; init; }
    public required string HostName { get; init; }
    public required int Port { get; init; }
    public required List<IPAddress> Addresses { get; init; }
    public required Dictionary<string, string> Metadata { get; init; }
    public required DateTime Timestamp { get; init; }
}
