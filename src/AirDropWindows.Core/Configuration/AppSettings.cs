namespace AirDropWindows.Core.Configuration;

/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Device settings
    /// </summary>
    public DeviceSettings Device { get; set; } = new();

    /// <summary>
    /// Network settings
    /// </summary>
    public NetworkSettings Network { get; set; } = new();

    /// <summary>
    /// Transfer settings
    /// </summary>
    public TransferSettings Transfer { get; set; } = new();

    /// <summary>
    /// Security settings
    /// </summary>
    public SecuritySettings Security { get; set; } = new();

    /// <summary>
    /// Logging settings
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();
}

/// <summary>
/// Device configuration
/// </summary>
public class DeviceSettings
{
    public string DeviceId { get; set; } = Guid.NewGuid().ToString();
    public string DeviceName { get; set; } = Environment.MachineName;
    public string DiscoveryMode { get; set; } = "Everyone";
    public bool AutoAccept { get; set; } = false;
    public string DefaultSaveLocation { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AirDrop");
}

/// <summary>
/// Network configuration
/// </summary>
public class NetworkSettings
{
    public bool UseWiFiDirect { get; set; } = true;
    public bool UseStandardWiFi { get; set; } = true;
    public int Port { get; set; } = 8771; // Default AirDrop port
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int DiscoveryIntervalSeconds { get; set; } = 10;
    public int DeviceExpirationSeconds { get; set; } = 60;
}

/// <summary>
/// Transfer configuration
/// </summary>
public class TransferSettings
{
    public int MaxConcurrentTransfers { get; set; } = 3;
    public long MaxFileSize { get; set; } = 5L * 1024 * 1024 * 1024; // 5 GB
    public int BufferSize { get; set; } = 81920; // 80 KB
    public bool CreateArchiveForMultipleFiles { get; set; } = true;
    public bool PreserveFileTimestamps { get; set; } = true;
    public int TransferTimeoutMinutes { get; set; } = 30;
}

/// <summary>
/// Security configuration
/// </summary>
public class SecuritySettings
{
    public bool RequireApproval { get; set; } = true;
    public bool AcceptSelfSignedCertificates { get; set; } = true;
    public int CertificateValidityDays { get; set; } = 365;
    public bool EnableTlsMutualAuth { get; set; } = true;
    public string CertificateStoreName { get; set; } = "My";
    public string CertificateStoreLocation { get; set; } = "CurrentUser";
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingSettings
{
    public string MinimumLevel { get; set; } = "Information";
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnableFileLogging { get; set; } = true;
    public string LogDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AirDropWindows", "Logs");
    public int RetainedFileCountLimit { get; set; } = 7;
    public long FileSizeLimitBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
}
