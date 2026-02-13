using System.Text.Json;
using Microsoft.AspNetCore.Http;
using AirDropWindows.Core.Interfaces;
using Serilog;

namespace AirDropWindows.Protocol.Http.Endpoints;

/// <summary>
/// Handles /Discover endpoint - Initial handshake with device identity
/// </summary>
public static class DiscoverEndpoint
{
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        ILogger logger,
        ISecurityService securityService)
    {
        try
        {
            logger.Debug("Received /Discover request from {RemoteIp}", context.Connection.RemoteIpAddress);

            // Read request body
            var requestBody = await JsonSerializer.DeserializeAsync<DiscoverRequest>(
                context.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (requestBody == null)
            {
                logger.Warning("/Discover: Invalid request body");
                return Results.BadRequest(new { error = "Invalid request" });
            }

            logger.Information("/Discover: Device '{DeviceName}' initiated handshake", requestBody.SenderComputerName);

            // Build response with our device identity
            var response = new DiscoverResponse
            {
                ReceiverComputerName = Environment.MachineName,
                ReceiverModelName = "Windows PC",
                ReceiverMediaCapabilities = new MediaCapabilities
                {
                    SupportsFiles = true,
                    SupportsPhotos = true,
                    SupportsVideos = true,
                    SupportsContacts = false,
                    SupportsUrls = true
                }
            };

            logger.Debug("/Discover: Sending response with device identity");
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "/Discover: Error processing request");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}

public class DiscoverRequest
{
    public string? SenderComputerName { get; set; }
    public string? SenderModelName { get; set; }
    public string? SenderID { get; set; }
}

public class DiscoverResponse
{
    public string? ReceiverComputerName { get; set; }
    public string? ReceiverModelName { get; set; }
    public MediaCapabilities? ReceiverMediaCapabilities { get; set; }
}

public class MediaCapabilities
{
    public bool SupportsFiles { get; set; }
    public bool SupportsPhotos { get; set; }
    public bool SupportsVideos { get; set; }
    public bool SupportsContacts { get; set; }
    public bool SupportsUrls { get; set; }
}
