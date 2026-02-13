# AirDrop Windows - Claude Code Guide

## Project Overview

This project implements a Windows application that enables AirDrop-like file transfers with iOS and macOS devices. It uses C# / .NET 10.0 with Avalonia UI and implements the AirDrop protocol using Wi-Fi Direct as a replacement for Apple's proprietary AWDL protocol.

## Project Structure

```
AirDropWindows/
├── src/
│   ├── AirDropWindows.Core/          # Core models, interfaces, and configuration
│   ├── AirDropWindows.Discovery/     # BLE and mDNS device discovery
│   ├── AirDropWindows.Network/       # Wi-Fi Direct and network transport
│   ├── AirDropWindows.Security/      # Certificate management and TLS
│   ├── AirDropWindows.Protocol/      # HTTP/2 AirDrop protocol implementation
│   ├── AirDropWindows.Services/      # Application services layer
│   └── AirDropWindows.UI/            # Avalonia UI application
├── tests/                            # Unit and integration tests
└── docs/                             # Documentation

```

## Key Technologies

- **Framework**: .NET 10.0 targeting Windows 10.0.19041+
- **UI**: Avalonia 11.3 (MVVM pattern with CommunityToolkit.Mvvm)
- **Discovery**: Makaretu.Dns.Multicast (mDNS/Bonjour) + Windows Bluetooth APIs
- **Networking**: Windows.Devices.WiFiDirect (WinRT APIs)
- **Security**: BouncyCastle for cryptography, TLS 1.2+ with mutual auth
- **HTTP**: ASP.NET Core Kestrel for HTTP/2 server
- **Logging**: Serilog with console and file sinks
- **DI**: Microsoft.Extensions.DependencyInjection

## Build & Run

```bash
# Build the solution
cd src
dotnet build

# Run the application
cd AirDropWindows.UI
dotnet run

# Run tests (once implemented)
cd ../.. && dotnet test
```

## Log Locations

- Application logs: `%LOCALAPPDATA%\AirDropWindows\Logs\`
- Configuration: `%APPDATA%\AirDropWindows\config.json`
- Certificates: Windows Certificate Store (My/Personal)

## Implementation Status

### Phase 1: Foundation ✅ (COMPLETE)
- [x] Solution structure created
- [x] Avalonia UI application configured
- [x] NuGet packages added
- [x] Logging infrastructure (Serilog)
- [x] Core models and interfaces
- [x] Configuration service

### Phase 2: Network Discovery (Upcoming)
- [ ] BLE advertisement scanner
- [ ] mDNS service browser/publisher
- [ ] Device registry

### Phase 3: Network Transport (Upcoming)
- [ ] Wi-Fi Direct implementation
- [ ] Standard Wi-Fi fallback
- [ ] Connection state machine

### Phase 4: Security (Upcoming)
- [ ] Certificate manager
- [ ] TLS server/client
- [ ] Identity manager

### Phase 5: File Transfer Protocol (Upcoming)
- [ ] HTTP/2 AirDrop server
- [ ] AirDrop client
- [ ] File streaming

### Phase 6: User Interface (Upcoming)
- [ ] Device discovery UI
- [ ] File selection
- [ ] Transfer progress
- [ ] Settings panel

## AirDrop Protocol Notes

### Discovery Flow
1. **BLE Advertisement**: Broadcast presence to wake Apple devices
2. **mDNS Service**: Advertise `_airdrop._tcp` service with device metadata
3. **Connection**: Establish Wi-Fi Direct P2P connection (replaces AWDL)
4. **TLS Handshake**: Mutual authentication with self-signed certs
5. **HTTP/2 Transfer**: `/Discover`, `/Ask`, `/Upload` endpoints

### Protocol Endpoints
- **POST /Discover**: Initial handshake, exchange device identities
- **POST /Ask**: Request permission to send files, includes file metadata
- **POST /Upload**: Multipart file transfer with progress tracking

## Known Limitations

1. **Manual AirDrop Activation**: Users must open AirDrop on Apple devices
2. **Certificate Warnings**: Apple devices show security warnings
3. **No Contact Matching**: Cannot access iCloud contacts
4. **Performance**: Wi-Fi Direct may be slower than AWDL
5. **Windows Only**: This implementation is Windows-specific

## Development Notes

- All projects targeting `net10.0-windows10.0.19041.0` except Core and Security
- Windows Runtime APIs accessed via C#/WinRT projection (`UseWinRT=true`)
- Avalonia uses compiled bindings by default for performance
- Solution uses NuGet package reference format

## Critical Files

- `src/AirDropWindows.Discovery/mDNS/MdnsServiceBrowser.cs` - Service discovery
- `src/AirDropWindows.Network/Transports/WiFiDirectTransport.cs` - Network transport
- `src/AirDropWindows.Protocol/Http/AirDropHttpServer.cs` - HTTP/2 server
- `src/AirDropWindows.Security/TlsServerProvider.cs` - TLS authentication
- `src/AirDropWindows.Services/TransferManager.cs` - Transfer orchestration

## References

- [Implementation Plan](../.claude/plans/cuddly-percolating-boot.md)
- [OpenDrop](https://github.com/seemoo-lab/opendrop) - Python AirDrop implementation
- [Open Wireless Link](https://owlink.org/) - AWDL protocol research
- [Avalonia Docs](https://docs.avaloniaui.net/)
- [Windows WinRT APIs](https://learn.microsoft.com/en-us/windows/uwp/api/)
