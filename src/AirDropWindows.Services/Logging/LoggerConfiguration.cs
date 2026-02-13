using Serilog;
using Serilog.Events;
using AirDropWindows.Core.Configuration;

namespace AirDropWindows.Services.Logging;

/// <summary>
/// Configures Serilog logging for the application
/// </summary>
public static class LoggerConfiguration
{
    /// <summary>
    /// Configure Serilog with settings from AppSettings
    /// </summary>
    public static ILogger CreateLogger(LoggingSettings settings)
    {
        var logDirectory = settings.LogDirectory;

        // Ensure log directory exists
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var logLevel = ParseLogLevel(settings.MinimumLevel);

        var config = new Serilog.LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", "AirDropWindows");

        // Console logging
        if (settings.EnableConsoleLogging)
        {
            config.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        }

        // File logging with rolling
        if (settings.EnableFileLogging)
        {
            var logFile = Path.Combine(logDirectory, "airdrop-.log");
            config.WriteTo.File(
                logFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: settings.RetainedFileCountLimit,
                fileSizeLimitBytes: settings.FileSizeLimitBytes,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
        }

        return config.CreateLogger();
    }

    /// <summary>
    /// Parse log level string to LogEventLevel
    /// </summary>
    private static LogEventLevel ParseLogLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}
