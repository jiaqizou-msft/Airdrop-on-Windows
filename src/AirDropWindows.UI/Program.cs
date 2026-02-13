using Avalonia;
using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using AirDropWindows.Core.Configuration;
using AirDropWindows.Services.Configuration;
using AirDropWindows.Services.Extensions;

namespace AirDropWindows.UI;

sealed class Program
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Initialize logging with default settings
            var defaultSettings = new AppSettings();
            Log.Logger = Services.Logging.LoggerConfiguration.CreateLogger(defaultSettings.Logging);

            Log.Information("Starting AirDrop Windows v{Version}", Constants.Version);
            Log.Information("Platform: {Platform}, OS: {OS}", Environment.OSVersion.Platform, Environment.OSVersion);

            // Load configuration first
            var configService = new ConfigurationService(Log.Logger);
            var settings = configService.LoadSettingsAsync().GetAwaiter().GetResult();

            // Reconfigure logger with loaded settings
            Log.Logger = Services.Logging.LoggerConfiguration.CreateLogger(settings.Logging);
            Log.Information("Configuration loaded, logger reconfigured");

            // Build DI container with loaded settings
            ServiceProvider = BuildServiceProvider(settings);

            // Start Avalonia application
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Build the dependency injection service provider
    /// </summary>
    private static IServiceProvider BuildServiceProvider(AppSettings settings)
    {
        var services = new ServiceCollection();

        // Register AirDrop services
        services.AddAirDropServices(Log.Logger, settings);

        return services.BuildServiceProvider();
    }
}
