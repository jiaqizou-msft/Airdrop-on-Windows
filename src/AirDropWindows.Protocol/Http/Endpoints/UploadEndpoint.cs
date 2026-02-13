using Microsoft.AspNetCore.Http;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Configuration;
using Serilog;

namespace AirDropWindows.Protocol.Http.Endpoints;

/// <summary>
/// Handles /Upload endpoint - Multipart file transfer
/// </summary>
public static class UploadEndpoint
{
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        ILogger logger,
        ITransferService transferService,
        AppSettings settings)
    {
        try
        {
            logger.Debug("Received /Upload request from {RemoteIp}", context.Connection.RemoteIpAddress);

            if (!context.Request.HasFormContentType)
            {
                logger.Warning("/Upload: Request does not have multipart/form-data content type");
                return Results.BadRequest(new { error = "Expected multipart/form-data" });
            }

            var form = await context.Request.ReadFormAsync();
            var files = form.Files;

            if (files.Count == 0)
            {
                logger.Warning("/Upload: No files in upload request");
                return Results.BadRequest(new { error = "No files uploaded" });
            }

            logger.Information("/Upload: Receiving {FileCount} file(s)", files.Count);

            var savedFiles = new List<string>();
            var saveDirectory = settings.Device.DefaultSaveLocation;

            // Ensure save directory exists
            Directory.CreateDirectory(saveDirectory);

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    logger.Warning("/Upload: Skipping empty file {FileName}", file.FileName);
                    continue;
                }

                // Sanitize filename to prevent directory traversal
                var safeFileName = Path.GetFileName(file.FileName);
                var savePath = Path.Combine(saveDirectory, safeFileName);

                // Handle duplicate filenames
                if (File.Exists(savePath))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
                    var extension = Path.GetExtension(safeFileName);
                    var counter = 1;

                    do
                    {
                        safeFileName = $"{nameWithoutExt} ({counter}){extension}";
                        savePath = Path.Combine(saveDirectory, safeFileName);
                        counter++;
                    } while (File.Exists(savePath));
                }

                logger.Debug("/Upload: Saving file {FileName} ({Size} bytes) to {Path}",
                    file.FileName, file.Length, savePath);

                // Save file to disk
                using (var stream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, settings.Transfer.BufferSize, useAsync: true))
                {
                    await file.CopyToAsync(stream);
                }

                // Preserve file timestamps if configured
                if (settings.Transfer.PreserveFileTimestamps && form.ContainsKey($"{file.Name}_timestamp"))
                {
                    if (DateTime.TryParse(form[$"{file.Name}_timestamp"], out var timestamp))
                    {
                        File.SetLastWriteTime(savePath, timestamp);
                    }
                }

                savedFiles.Add(savePath);
                logger.Information("/Upload: Successfully saved file {FileName} to {Path}", file.FileName, savePath);
            }

            var response = new UploadResponse
            {
                Success = true,
                FilesReceived = savedFiles.Count,
                Message = $"Successfully received {savedFiles.Count} file(s)"
            };

            logger.Information("/Upload: Transfer complete, {Count} file(s) saved to {Directory}",
                savedFiles.Count, saveDirectory);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "/Upload: Error processing file upload");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}

public class UploadResponse
{
    public bool Success { get; set; }
    public int FilesReceived { get; set; }
    public string? Message { get; set; }
}
