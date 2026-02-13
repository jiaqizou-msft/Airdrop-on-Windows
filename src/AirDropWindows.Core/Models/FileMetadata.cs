namespace AirDropWindows.Core.Models;

/// <summary>
/// Represents metadata for a file to be transferred
/// </summary>
public class FileMetadata
{
    /// <summary>
    /// Original file name
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// File MIME type (e.g., "image/jpeg", "text/plain")
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Apple UTI type (e.g., "public.jpeg", "public.plain-text")
    /// </summary>
    public string? FileType { get; set; }

    /// <summary>
    /// Path within the archive (for multiple files)
    /// </summary>
    public string? FileBomPath { get; set; }

    /// <summary>
    /// Whether this is a directory
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// Source file path (for sending)
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// Destination path (for receiving)
    /// </summary>
    public string? DestinationPath { get; set; }

    /// <summary>
    /// File creation timestamp
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// File modification timestamp
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// SHA256 hash of file content (optional)
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// Thumbnail data (for images/videos)
    /// </summary>
    public byte[]? ThumbnailData { get; set; }

    /// <summary>
    /// Whether to convert media formats
    /// </summary>
    public bool ConvertMediaFormats { get; set; }
}
