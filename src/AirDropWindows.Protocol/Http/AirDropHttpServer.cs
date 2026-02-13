using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Configuration;
using AirDropWindows.Protocol.Http.Endpoints;
using Serilog;

namespace AirDropWindows.Protocol.Http;

/// <summary>
/// HTTP/2 server implementing AirDrop REST protocol
/// </summary>
public class AirDropHttpServer : IDisposable
{
    private readonly ILogger _logger;
    private readonly ISecurityService _securityService;
    private readonly ITransferService _transferService;
    private readonly AppSettings _settings;
    private WebApplication? _app;
    private bool _isRunning;

    public AirDropHttpServer(
        ILogger logger,
        ISecurityService securityService,
        ITransferService transferService,
        AppSettings settings)
    {
        _logger = logger;
        _securityService = securityService;
        _transferService = transferService;
        _settings = settings;
    }

    /// <summary>
    /// Start the HTTP/2 server
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.Warning("HTTP server already running");
            return;
        }

        _logger.Information("Starting AirDrop HTTP/2 server on port {Port}", _settings.Network.Port);

        var builder = WebApplication.CreateBuilder();

        // Configure Kestrel for HTTP/2 with TLS
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Any, _settings.Network.Port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;

                // Configure TLS with mutual authentication
                var certificate = _securityService.GetOrCreateCertificateAsync().GetAwaiter().GetResult();
                listenOptions.UseHttps(certificate, httpsOptions =>
                {
                    httpsOptions.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.AllowCertificate;
                    httpsOptions.AllowAnyClientCertificate(); // Accept self-signed certs
                });
            });
        });

        // Register services
        builder.Services.AddSingleton(_logger);
        builder.Services.AddSingleton(_securityService);
        builder.Services.AddSingleton(_transferService);
        builder.Services.AddSingleton(_settings);

        _app = builder.Build();

        // Register AirDrop endpoints
        RegisterEndpoints(_app);

        await _app.StartAsync(cancellationToken);
        _isRunning = true;

        _logger.Information("AirDrop HTTP/2 server started successfully");
    }

    /// <summary>
    /// Stop the HTTP/2 server
    /// </summary>
    public async Task StopAsync()
    {
        if (_app != null && _isRunning)
        {
            _logger.Information("Stopping AirDrop HTTP/2 server");
            await _app.StopAsync();
            _isRunning = false;
        }
    }

    /// <summary>
    /// Register AirDrop protocol endpoints
    /// </summary>
    private void RegisterEndpoints(WebApplication app)
    {
        // POST /Discover - Initial handshake
        app.MapPost("/Discover", DiscoverEndpoint.HandleAsync);

        // POST /Ask - Request permission to send files
        app.MapPost("/Ask", AskEndpoint.HandleAsync);

        // POST /Upload - Multipart file transfer
        app.MapPost("/Upload", UploadEndpoint.HandleAsync);

        _logger.Debug("Registered AirDrop protocol endpoints: /Discover, /Ask, /Upload");
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _app?.DisposeAsync().GetAwaiter().GetResult();
    }
}
