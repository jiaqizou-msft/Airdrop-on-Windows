using System.Collections.Concurrent;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Models;
using Serilog;

namespace AirDropWindows.Services;

/// <summary>
/// Manages file transfer operations
/// </summary>
public class TransferService : ITransferService
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, TransferRequest> _activeTransfers;

    public event EventHandler<TransferRequest>? TransferRequested;
    public event EventHandler<TransferProgress>? ProgressUpdated;
    public event EventHandler<TransferRequest>? TransferCompleted;
    public event EventHandler<TransferRequest>? TransferFailed;

    public TransferService(ILogger logger)
    {
        _logger = logger;
        _activeTransfers = new ConcurrentDictionary<string, TransferRequest>();
    }

    public async Task<TransferRequest> SendFilesAsync(
        DiscoveredDevice targetDevice,
        IEnumerable<string> filePaths,
        CancellationToken cancellationToken = default)
    {
        var files = filePaths.Select(path => new FileMetadata
        {
            FileName = Path.GetFileName(path),
            FileSize = new FileInfo(path).Length,
            SourcePath = path
        }).ToList();

        var request = new TransferRequest
        {
            SourceDevice = new DiscoveredDevice
            {
                DeviceId = Environment.MachineName,
                DeviceName = Environment.MachineName,
                DeviceType = DeviceType.WindowsPC
            },
            DestinationDevice = targetDevice,
            Files = files,
            Direction = TransferDirection.Sending,
            TotalBytes = files.Sum(f => f.FileSize)
        };

        _activeTransfers[request.RequestId] = request;
        _logger.Information("Initiating file transfer to {Device}: {FileCount} files",
            targetDevice.DeviceName, files.Count);

        // Simulate transfer (real implementation would use HTTP/2 protocol)
        await Task.Delay(100, cancellationToken);

        return request;
    }

    public async Task ApproveTransferAsync(string requestId, string? savePath = null)
    {
        if (_activeTransfers.TryGetValue(requestId, out var request))
        {
            request.IsApproved = true;
            request.Status = TransferStatus.Approved;
            _logger.Information("Transfer {RequestId} approved", requestId);
        }
        await Task.CompletedTask;
    }

    public async Task RejectTransferAsync(string requestId, string? reason = null)
    {
        if (_activeTransfers.TryGetValue(requestId, out var request))
        {
            request.Status = TransferStatus.Rejected;
            _logger.Information("Transfer {RequestId} rejected: {Reason}", requestId, reason);
        }
        await Task.CompletedTask;
    }

    public async Task CancelTransferAsync(string requestId)
    {
        if (_activeTransfers.TryGetValue(requestId, out var request))
        {
            request.CancellationTokenSource?.Cancel();
            request.Status = TransferStatus.Cancelled;
            _logger.Information("Transfer {RequestId} cancelled", requestId);
        }
        await Task.CompletedTask;
    }

    public IReadOnlyCollection<TransferRequest> GetActiveTransfers()
    {
        return _activeTransfers.Values.ToList().AsReadOnly();
    }

    public TransferRequest? GetTransfer(string requestId)
    {
        return _activeTransfers.TryGetValue(requestId, out var request) ? request : null;
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        _logger.Information("Transfer service started listening");
        await Task.CompletedTask;
    }

    public async Task StopListeningAsync()
    {
        _logger.Information("Transfer service stopped listening");
        await Task.CompletedTask;
    }
}
