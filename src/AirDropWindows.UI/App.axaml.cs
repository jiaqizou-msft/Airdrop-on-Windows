using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using AirDropWindows.UI.ViewModels;
using AirDropWindows.UI.Views;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Configuration;
using Serilog;

namespace AirDropWindows.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Get services from DI container
            var serviceProvider = Program.ServiceProvider;
            if (serviceProvider == null)
            {
                throw new InvalidOperationException("Service provider not initialized");
            }

            var logger = serviceProvider.GetRequiredService<ILogger>();
            var discoveryService = serviceProvider.GetRequiredService<IDiscoveryService>();
            var transferService = serviceProvider.GetRequiredService<ITransferService>();
            var configService = serviceProvider.GetRequiredService<Services.Configuration.ConfigurationService>();
            var settings = configService.LoadSettingsAsync().GetAwaiter().GetResult();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(logger, discoveryService, transferService, settings),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}