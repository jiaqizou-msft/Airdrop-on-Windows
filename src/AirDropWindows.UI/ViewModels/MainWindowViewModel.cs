using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Models;
using AirDropWindows.Core.Configuration;
using Serilog;

namespace AirDropWindows.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IDiscoveryService _discoveryService;
    private readonly ITransferService _transferService;
    private readonly AppSettings _settings;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isDiscovering;

    [ObservableProperty]
    private DiscoveredDevice? _selectedDevice;

    public ObservableCollection<DiscoveredDevice> DiscoveredDevices { get; }
    public ObservableCollection<TransferRequest> ActiveTransfers { get; }

    public MainWindowViewModel(
        ILogger logger,
        IDiscoveryService discoveryService,
        ITransferService transferService,
        AppSettings settings)
    {
        _logger = logger;
        _discoveryService = discoveryService;
        _transferService = transferService;
        _settings = settings;

        DiscoveredDevices = new ObservableCollection<DiscoveredDevice>();
        ActiveTransfers = new ObservableCollection<TransferRequest>();

        // Subscribe to discovery events
        _discoveryService.DeviceDiscovered += OnDeviceDiscovered;
        _discoveryService.DeviceLost += OnDeviceLost;

        // Subscribe to transfer events
        _transferService.TransferRequested += OnTransferRequested;
        _transferService.ProgressUpdated += OnProgressUpdated;
        _transferService.TransferCompleted += OnTransferCompleted;
        _transferService.TransferFailed += OnTransferFailed;
    }

    [RelayCommand]
    private async Task StartDiscoveryAsync()
    {
        try
        {
            IsDiscovering = true;
            StatusMessage = "Discovering devices...";
            _logger.Information("Starting device discovery");

            await _discoveryService.StartAsync();

            StatusMessage = "Listening for AirDrop devices";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start discovery");
            StatusMessage = $"Error: {ex.Message}";
            IsDiscovering = false;
        }
    }

    [RelayCommand]
    private async Task StopDiscoveryAsync()
    {
        try
        {
            _logger.Information("Stopping device discovery");
            await _discoveryService.StopAsync();

            IsDiscovering = false;
            StatusMessage = "Discovery stopped";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to stop discovery");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SelectFilesAsync()
    {
        if (SelectedDevice == null)
        {
            StatusMessage = "Please select a device first";
            return;
        }

        // This would open a file picker dialog in a real implementation
        // For now, we'll just log the action
        _logger.Information("File selection requested for device {Device}", SelectedDevice.DeviceName);
        StatusMessage = $"Ready to send to {SelectedDevice.DeviceName}";

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void RefreshDevices()
    {
        _logger.Debug("Refreshing device list");
        var devices = _discoveryService.GetDiscoveredDevices();

        DiscoveredDevices.Clear();
        foreach (var device in devices)
        {
            DiscoveredDevices.Add(device);
        }

        StatusMessage = $"Found {DiscoveredDevices.Count} device(s)";
    }

    private void OnDeviceDiscovered(object? sender, DiscoveredDevice device)
    {
        _logger.Information("Device discovered: {Device}", device.DeviceName);

        // Update on UI thread
        if (!DiscoveredDevices.Any(d => d.DeviceId == device.DeviceId))
        {
            DiscoveredDevices.Add(device);
        }

        StatusMessage = $"Found {DiscoveredDevices.Count} device(s)";
    }

    private void OnDeviceLost(object? sender, DiscoveredDevice device)
    {
        _logger.Information("Device lost: {DeviceId}", device.DeviceId);

        var existingDevice = DiscoveredDevices.FirstOrDefault(d => d.DeviceId == device.DeviceId);
        if (existingDevice != null)
        {
            DiscoveredDevices.Remove(existingDevice);
        }

        StatusMessage = $"Found {DiscoveredDevices.Count} device(s)";
    }

    private void OnTransferRequested(object? sender, TransferRequest request)
    {
        _logger.Information("Transfer requested: {RequestId}", request.RequestId);
        ActiveTransfers.Add(request);
        StatusMessage = $"Transfer requested from {request.SourceDevice.DeviceName}";
    }

    private void OnProgressUpdated(object? sender, TransferProgress progress)
    {
        _logger.Debug("Transfer progress: {Progress}%", progress.Progress);
        StatusMessage = $"Transfer {progress.Progress:F1}% complete";
    }

    private void OnTransferCompleted(object? sender, TransferRequest request)
    {
        _logger.Information("Transfer completed: {RequestId}", request.RequestId);
        StatusMessage = $"Transfer from {request.SourceDevice.DeviceName} completed";

        var transfer = ActiveTransfers.FirstOrDefault(t => t.RequestId == request.RequestId);
        if (transfer != null)
        {
            ActiveTransfers.Remove(transfer);
        }
    }

    private void OnTransferFailed(object? sender, TransferRequest request)
    {
        _logger.Error("Transfer failed: {RequestId}", request.RequestId);
        StatusMessage = $"Transfer from {request.SourceDevice.DeviceName} failed";

        var transfer = ActiveTransfers.FirstOrDefault(t => t.RequestId == request.RequestId);
        if (transfer != null)
        {
            ActiveTransfers.Remove(transfer);
        }
    }

    public void Cleanup()
    {
        _discoveryService.DeviceDiscovered -= OnDeviceDiscovered;
        _discoveryService.DeviceLost -= OnDeviceLost;
        _transferService.TransferRequested -= OnTransferRequested;
        _transferService.ProgressUpdated -= OnProgressUpdated;
        _transferService.TransferCompleted -= OnTransferCompleted;
        _transferService.TransferFailed -= OnTransferFailed;
    }
}
