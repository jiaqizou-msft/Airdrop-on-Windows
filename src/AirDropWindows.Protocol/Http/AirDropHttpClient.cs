using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Models;
using AirDropWindows.Protocol.Http.Endpoints;
using Serilog;

namespace AirDropWindows.Protocol.Http;

/// <summary>
/// HTTP/2 client for sending files via AirDrop protocol
/// </summary>
public class AirDropHttpClient : IDisposable
{
    private readonly ILogger _logger;
    private readonly ISecurityService _securityService;
    private readonly HttpClient _httpClient;

    public AirDropHttpClient(ILogger logger, ISecurityService securityService)
    {
        _logger = logger;
        _securityService = securityService;

        // Configure HTTP client with TLS and certificate handling
        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                // Accept self-signed certificates from Apple devices
                RemoteCertificateValidationCallback = ValidateServerCertificate,
                // Present our certificate for mutual TLS
                ClientCertificates = new X509CertificateCollection
                {
                    securityService.GetOrCreateCertificateAsync().GetAwaiter().GetResult()
                }
            }
        };

        _httpClient = new HttpClient(handler)
        {
            DefaultRequestVersion = new Version(2, 0),
            Timeout = TimeSpan.FromMinutes(30)
        };
    }

    /// <summary>
    /// Send Discover handshake to remote device
    /// </summary>
    public async Task<DiscoverResponse?> DiscoverAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Debug("Sending /Discover to {Url}", baseUrl);

            var request = new DiscoverRequest
            {
                SenderComputerName = Environment.MachineName,
                SenderModelName = "Windows PC",
                SenderID = Environment.MachineName
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{baseUrl}/Discover", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var discoverResponse = JsonSerializer.Deserialize<DiscoverResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.Information("Received /Discover response from {Device}", discoverResponse?.ReceiverComputerName);
            return discoverResponse;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during /Discover handshake with {Url}", baseUrl);
            return null;
        }
    }

    /// <summary>
    /// Send Ask request to request permission for file transfer
    /// </summary>
    public async Task<AskResponse?> AskAsync(string baseUrl, IEnumerable<FileMetadata> files, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Debug("Sending /Ask to {Url} with {FileCount} files", baseUrl, files.Count());

            var request = new AskRequest
            {
                SenderComputerName = Environment.MachineName,
                SenderID = Environment.MachineName,
                Files = files.Select(f => new AskRequest.FileInfo
                {
                    FileName = f.FileName,
                    FileSize = f.FileSize,
                    FileType = f.MimeType ?? "application/octet-stream",
                    FileIsDirectory = false
                })
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{baseUrl}/Ask", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var askResponse = JsonSerializer.Deserialize<AskResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.Information("Received /Ask response from {Device}, permission granted", askResponse?.ReceiverComputerName);
            return askResponse;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during /Ask request to {Url}", baseUrl);
            return null;
        }
    }

    /// <summary>
    /// Upload files to remote device
    /// </summary>
    public async Task<bool> UploadAsync(string baseUrl, IEnumerable<FileMetadata> files, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Information("Uploading {FileCount} file(s) to {Url}", files.Count(), baseUrl);

            using var content = new MultipartFormDataContent();

            long totalBytes = files.Sum(f => f.FileSize);

            foreach (var file in files)
            {
                if (!File.Exists(file.SourcePath))
                {
                    _logger.Warning("Source file not found: {Path}", file.SourcePath);
                    continue;
                }

                var fileStream = new FileStream(file.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.MimeType ?? "application/octet-stream");

                content.Add(streamContent, Path.GetFileName(file.FileName), file.FileName);

                // Add file timestamp metadata
                var timestamp = File.GetLastWriteTime(file.SourcePath);
                content.Add(new StringContent(timestamp.ToString("o")), $"{file.FileName}_timestamp");

                _logger.Debug("Added file to upload: {FileName} ({Size} bytes)", file.FileName, file.FileSize);
            }

            var response = await _httpClient.PostAsync($"{baseUrl}/Upload", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var uploadResponse = JsonSerializer.Deserialize<UploadResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.Information("Upload complete: {FilesReceived} file(s) received by remote device", uploadResponse?.FilesReceived);

            // Report completion
            if (progress != null)
            {
                progress.Report(new TransferProgress
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Progress = 100.0,
                    BytesTransferred = totalBytes,
                    TotalBytes = totalBytes,
                    TransferRate = 0,
                    TimeElapsed = TimeSpan.Zero,
                    CurrentFileName = null
                });
            }

            return uploadResponse?.Success ?? false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during /Upload to {Url}", baseUrl);
            return false;
        }
    }

    /// <summary>
    /// Validate server certificate (accept self-signed certificates)
    /// </summary>
    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        // Accept all certificates (self-signed from Apple devices)
        // In production, you might want to add certificate pinning or other validation
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        _logger.Debug("Server certificate validation: {Errors}", sslPolicyErrors);

        // Accept self-signed certificates
        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
        {
            _logger.Debug("Accepting self-signed certificate from remote device");
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
