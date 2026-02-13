using System.Text.Json;
using AirDropWindows.Core.Configuration;
using Serilog;

namespace AirDropWindows.Services.Configuration;

/// <summary>
/// Service for loading and saving application configuration
/// </summary>
public class ConfigurationService
{
    private readonly string _configFilePath;
    private readonly ILogger _logger;
    private AppSettings? _settings;

    public ConfigurationService(ILogger logger)
    {
        _logger = logger;
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AirDropWindows");

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _configFilePath = Path.Combine(appDataPath, "config.json");
    }

    /// <summary>
    /// Load settings from file, or create defaults if file doesn't exist
    /// </summary>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (_settings != null)
        {
            return _settings;
        }

        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json, GetJsonOptions());

                if (_settings != null)
                {
                    _logger.Information("Configuration loaded from {Path}", _configFilePath);
                    return _settings;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load configuration from {Path}, using defaults", _configFilePath);
        }

        // Create default settings
        _settings = new AppSettings();
        await SaveSettingsAsync(_settings);

        return _settings;
    }

    /// <summary>
    /// Save settings to file
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, GetJsonOptions());
            await File.WriteAllTextAsync(_configFilePath, json);
            _settings = settings;

            _logger.Information("Configuration saved to {Path}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save configuration to {Path}", _configFilePath);
            throw;
        }
    }

    /// <summary>
    /// Get current settings (loads if not already loaded)
    /// </summary>
    public async Task<AppSettings> GetSettingsAsync()
    {
        return _settings ?? await LoadSettingsAsync();
    }

    /// <summary>
    /// Update specific settings
    /// </summary>
    public async Task UpdateSettingsAsync(Action<AppSettings> updateAction)
    {
        var settings = await GetSettingsAsync();
        updateAction(settings);
        await SaveSettingsAsync(settings);
    }

    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    public async Task ResetToDefaultsAsync()
    {
        _settings = new AppSettings();
        await SaveSettingsAsync(_settings);
        _logger.Information("Configuration reset to defaults");
    }

    /// <summary>
    /// Get JSON serialization options
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
}
