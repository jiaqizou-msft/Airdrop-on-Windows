using AirDropWindows.Core.Models;

namespace AirDropWindows.Core.Interfaces;

/// <summary>
/// Service for handling file transfers
/// </summary>
public interface ITransferService
{
    /// <summary>
    /// Event raised when a new transfer request is received
    /// </summary>
    event EventHandler<TransferRequest>? TransferRequested;

    /// <summary>
    /// Event raised when transfer progress is updated
    /// </summary>
    event EventHandler<TransferProgress>? ProgressUpdated;

    /// <summary>
    /// Event raised when a transfer completes
    /// </summary>
    event EventHandler<TransferRequest>? TransferCompleted;

    /// <summary>
    /// Event raised when a transfer fails
    /// </summary>
    event EventHandler<TransferRequest>? TransferFailed;

    /// <summary>
    /// Send files to a device
    /// </summary>
    Task<TransferRequest> SendFilesAsync(
        DiscoveredDevice targetDevice,
        IEnumerable<string> filePaths,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve a pending transfer request
    /// </summary>
    Task ApproveTransferAsync(string requestId, string? savePath = null);

    /// <summary>
    /// Reject a pending transfer request
    /// </summary>
    Task RejectTransferAsync(string requestId, string? reason = null);

    /// <summary>
    /// Cancel an ongoing transfer
    /// </summary>
    Task CancelTransferAsync(string requestId);

    /// <summary>
    /// Get all active and pending transfers
    /// </summary>
    IReadOnlyCollection<TransferRequest> GetActiveTransfers();

    /// <summary>
    /// Get a specific transfer by ID
    /// </summary>
    TransferRequest? GetTransfer(string requestId);

    /// <summary>
    /// Start listening for incoming transfers
    /// </summary>
    Task StartListeningAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop listening for incoming transfers
    /// </summary>
    Task StopListeningAsync();
}

/// <summary>
/// Transfer progress information
/// </summary>
public class TransferProgress
{
    public required string RequestId { get; init; }
    public required double Progress { get; init; }
    public required long BytesTransferred { get; init; }
    public required long TotalBytes { get; init; }
    public required double TransferRate { get; init; } // bytes per second
    public required TimeSpan TimeElapsed { get; init; }
    public TimeSpan? TimeRemaining { get; init; }
    public string? CurrentFileName { get; init; }
}
