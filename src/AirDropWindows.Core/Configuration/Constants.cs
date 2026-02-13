namespace AirDropWindows.Core.Configuration;

/// <summary>
/// Application constants
/// </summary>
public static class Constants
{
    /// <summary>
    /// Application name
    /// </summary>
    public const string ApplicationName = "AirDrop Windows";

    /// <summary>
    /// Application version
    /// </summary>
    public const string Version = "0.1.0-alpha";

    /// <summary>
    /// AirDrop protocol constants
    /// </summary>
    public static class AirDrop
    {
        /// <summary>
        /// mDNS service type
        /// </summary>
        public const string ServiceType = "_airdrop._tcp";

        /// <summary>
        /// Default AirDrop port
        /// </summary>
        public const int DefaultPort = 8771;

        /// <summary>
        /// BLE service UUID (unofficial - reverse engineered)
        /// </summary>
        public const string BleServiceUuid = "0000af0a-0000-1000-8000-00805f9b34fb";

        /// <summary>
        /// HTTP/2 protocol endpoints
        /// </summary>
        public static class Endpoints
        {
            public const string Discover = "/Discover";
            public const string Ask = "/Ask";
            public const string Upload = "/Upload";
        }

        /// <summary>
        /// Supported UTI types mapping
        /// </summary>
        public static readonly Dictionary<string, string> UtiTypes = new()
        {
            { ".txt", "public.plain-text" },
            { ".jpg", "public.jpeg" },
            { ".jpeg", "public.jpeg" },
            { ".png", "public.png" },
            { ".gif", "public.gif" },
            { ".pdf", "com.adobe.pdf" },
            { ".doc", "com.microsoft.word.doc" },
            { ".docx", "org.openxmlformats.wordprocessingml.document" },
            { ".xls", "com.microsoft.excel.xls" },
            { ".xlsx", "org.openxmlformats.spreadsheetml.sheet" },
            { ".ppt", "com.microsoft.powerpoint.ppt" },
            { ".pptx", "org.openxmlformats.presentationml.presentation" },
            { ".mp3", "public.mp3" },
            { ".mp4", "public.mpeg-4" },
            { ".mov", "com.apple.quicktime-movie" },
            { ".zip", "public.zip-archive" },
            { ".json", "public.json" }
        };

        /// <summary>
        /// Bundle identifier
        /// </summary>
        public const string BundleId = "com.airdrop.windows";
    }

    /// <summary>
    /// Network constants
    /// </summary>
    public static class Network
    {
        /// <summary>
        /// Connection timeout in milliseconds
        /// </summary>
        public const int ConnectionTimeoutMs = 30000;

        /// <summary>
        /// Read/write timeout in milliseconds
        /// </summary>
        public const int ReadWriteTimeoutMs = 60000;

        /// <summary>
        /// Keep-alive interval in milliseconds
        /// </summary>
        public const int KeepAliveIntervalMs = 5000;

        /// <summary>
        /// Maximum retries for failed operations
        /// </summary>
        public const int MaxRetries = 3;
    }

    /// <summary>
    /// File transfer constants
    /// </summary>
    public static class Transfer
    {
        /// <summary>
        /// Default buffer size for file streaming (80 KB)
        /// </summary>
        public const int DefaultBufferSize = 81920;

        /// <summary>
        /// Progress update interval in milliseconds
        /// </summary>
        public const int ProgressUpdateIntervalMs = 100;

        /// <summary>
        /// Archive format for multiple files
        /// </summary>
        public const string ArchiveFormat = "zip";

        /// <summary>
        /// Temporary file prefix
        /// </summary>
        public const string TempFilePrefix = "airdrop_";
    }

    /// <summary>
    /// mDNS service metadata keys
    /// </summary>
    public static class MdnsKeys
    {
        public const string DeviceType = "deviceType";
        public const string Transport = "transport";
        public const string Capabilities = "capabilities";
        public const string Version = "version";
    }

    /// <summary>
    /// Certificate constants
    /// </summary>
    public static class Certificate
    {
        /// <summary>
        /// Certificate subject name format
        /// </summary>
        public const string SubjectNameFormat = "CN=AirDrop-{0}";

        /// <summary>
        /// Key size in bits
        /// </summary>
        public const int KeySize = 2048;

        /// <summary>
        /// Certificate validity period in days
        /// </summary>
        public const int ValidityDays = 365;

        /// <summary>
        /// Days before expiration to trigger renewal
        /// </summary>
        public const int RenewalThresholdDays = 30;
    }
}
