namespace AirDropWindows.Core.Models;

/// <summary>
/// Represents a file transfer request
/// </summary>
public class TransferRequest
{
    /// <summary>
    /// Unique transfer request ID
    /// </summary>
    public string RequestId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Source device
    /// </summary>
    public required DiscoveredDevice SourceDevice { get; init; }

    /// <summary>
    /// Destination device
    /// </summary>
    public required DiscoveredDevice DestinationDevice { get; init; }

    /// <summary>
    /// Files to be transferred
    /// </summary>
    public required List<FileMetadata> Files { get; init; }

    /// <summary>
    /// Transfer direction
    /// </summary>
    public TransferDirection Direction { get; init; }

    /// <summary>
    /// Current transfer status
    /// </summary>
    public TransferStatus Status { get; set; } = TransferStatus.Pending;

    /// <summary>
    /// Transfer progress (0.0 to 1.0)
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// Total bytes to transfer
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// Bytes transferred so far
    /// </summary>
    public long BytesTransferred { get; set; }

    /// <summary>
    /// Timestamp when transfer was initiated
    /// </summary>
    public DateTime InitiatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when transfer started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when transfer completed (success or failure)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if transfer failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether user approval is required
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Whether user has approved the transfer
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Cancellation token source for this transfer
    /// </summary>
    public CancellationTokenSource? CancellationTokenSource { get; set; }
}

/// <summary>
/// Transfer direction enumeration
/// </summary>
public enum TransferDirection
{
    Sending,
    Receiving
}

/// <summary>
/// Transfer status enumeration
/// </summary>
public enum TransferStatus
{
    Pending,
    AwaitingApproval,
    Approved,
    Rejected,
    Connecting,
    Transferring,
    Completed,
    Failed,
    Cancelled
}
