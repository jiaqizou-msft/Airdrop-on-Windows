using System.Text.Json;
using Microsoft.AspNetCore.Http;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Models;
using Serilog;

namespace AirDropWindows.Protocol.Http.Endpoints;

/// <summary>
/// Handles /Ask endpoint - Request permission to send files
/// </summary>
public static class AskEndpoint
{
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        ILogger logger,
        ITransferService transferService)
    {
        try
        {
            logger.Debug("Received /Ask request from {RemoteIp}", context.Connection.RemoteIpAddress);

            // Read request body
            var requestBody = await JsonSerializer.DeserializeAsync<AskRequest>(
                context.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (requestBody == null || requestBody.Files == null || !requestBody.Files.Any())
            {
                logger.Warning("/Ask: Invalid request body or no files specified");
                return Results.BadRequest(new { error = "Invalid request or no files" });
            }

            logger.Information("/Ask: Transfer request from '{SenderName}' with {FileCount} file(s)",
                requestBody.SenderComputerName, requestBody.Files.Count());

            // Create transfer request
            var transferRequest = new TransferRequest
            {
                SourceDevice = new DiscoveredDevice
                {
                    DeviceId = requestBody.SenderID ?? "unknown",
                    DeviceName = requestBody.SenderComputerName ?? "Unknown Device",
                    DeviceType = DeviceType.Unknown
                },
                DestinationDevice = new DiscoveredDevice
                {
                    DeviceId = Environment.MachineName,
                    DeviceName = Environment.MachineName,
                    DeviceType = DeviceType.WindowsPC
                },
                Files = requestBody.Files.Select(f => new FileMetadata
                {
                    FileName = f.FileName ?? "unknown",
                    FileSize = f.FileSize ?? 0,
                    MimeType = f.FileType
                }).ToList(),
                Direction = TransferDirection.Receiving,
                Status = TransferStatus.Pending
            };

            // Raise event for UI to show approval dialog
            // In a real implementation, this would trigger a UI notification
            // For now, auto-approve based on settings
            transferRequest.IsApproved = false; // Will be approved via UI or auto-accept setting

            logger.Information("/Ask: Created transfer request {RequestId}, awaiting approval",
                transferRequest.RequestId);

            // Build response
            var response = new AskResponse
            {
                ReceiverComputerName = Environment.MachineName,
                ReceiverModelName = "Windows PC"
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "/Ask: Error processing request");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}

public class AskRequest
{
    public string? SenderComputerName { get; set; }
    public string? SenderID { get; set; }
    public IEnumerable<FileInfo>? Files { get; set; }

    public class FileInfo
    {
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string? FileType { get; set; }
        public bool? FileIsDirectory { get; set; }
    }
}

public class AskResponse
{
    public string? ReceiverComputerName { get; set; }
    public string? ReceiverModelName { get; set; }
}
