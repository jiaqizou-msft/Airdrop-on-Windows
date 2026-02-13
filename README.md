# AirDrop for Windows

A Windows application that enables AirDrop-like file transfers with iOS and macOS devices.

## Overview

This project implements the AirDrop protocol on Windows using C# / .NET 10.0, allowing seamless file sharing between Windows PCs and Apple devices. Since Apple's proprietary AWDL (Apple Wireless Direct Link) protocol is not available on Windows, this implementation uses Wi-Fi Direct as the primary transport layer with standard Wi-Fi as a fallback.

## Features (Planned)

- ✅ Discovery of nearby iOS and macOS devices via BLE and mDNS
- ✅ Secure file transfers using TLS 1.2+ with mutual authentication
- ✅ Support for single and multiple file transfers
- ✅ Transfer progress tracking and cancellation
- ✅ Modern Windows UI built with Avalonia
- ✅ Automatic network transport selection (Wi-Fi Direct or standard Wi-Fi)

## Technology Stack

- **.NET 10.0** - Latest .NET framework
- **Avalonia UI 11.3** - Cross-platform XAML-based UI framework
- **Windows.Devices.Bluetooth** - BLE advertisement scanning/broadcasting
- **Windows.Devices.WiFiDirect** - P2P networking (replaces AWDL)
- **Makaretu.Dns.Multicast** - mDNS/Bonjour service discovery
- **ASP.NET Core Kestrel** - HTTP/2 server for file transfer protocol
- **BouncyCastle** - Advanced cryptography operations
- **Serilog** - Structured logging

## Architecture

```
┌─────────────────────────────────────────────┐
│     Presentation Layer (Avalonia UI)       │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  Application Services                       │
│  - Transfer Manager                         │
│  - Device Manager                           │
│  - Settings Manager                         │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  AirDrop Protocol Layer                     │
│  - Discovery Service (BLE + mDNS)           │
│  - Pairing Service (TLS)                    │
│  - Transfer Service (HTTP/2)                │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│  Network Transport Layer                    │
│  - Wi-Fi Direct Provider (Primary)          │
│  - Standard Wi-Fi Provider (Fallback)       │
└─────────────────────────────────────────────┘
```

## Project Structure

```
AirDropWindows/
├── src/
│   ├── AirDropWindows.Core/          # Core models, interfaces, configuration
│   ├── AirDropWindows.Discovery/     # BLE and mDNS device discovery
│   ├── AirDropWindows.Network/       # Wi-Fi Direct and network transport
│   ├── AirDropWindows.Security/      # Certificate management and TLS
│   ├── AirDropWindows.Protocol/      # HTTP/2 AirDrop protocol implementation
│   ├── AirDropWindows.Services/      # Application services layer
│   └── AirDropWindows.UI/            # Avalonia UI application
├── tests/                            # Unit and integration tests
├── docs/                             # Documentation
├── CLAUDE.md                         # Development guide
└── README.md                         # This file
```

## Requirements

- **Windows 10/11** (Build 19041 or later)
- **.NET 10.0 SDK** or later
- **Bluetooth** adapter (for device discovery)
- **Wi-Fi** adapter with Wi-Fi Direct support (recommended)

## Building

```bash
# Clone the repository
git clone <repository-url>
cd airdrop-emulator

# Restore dependencies and build
cd src
dotnet restore
dotnet build

# Run the application
cd AirDropWindows.UI
dotnet run
```

## Usage

1. **Start the application** - Launch AirDrop Windows on your PC
2. **Open AirDrop on Apple device** - Set to "Everyone" for 10 minutes
3. **Select files to send** - Choose files in AirDrop Windows
4. **Select target device** - Your Apple device should appear in the list
5. **Accept on Apple device** - Approve the transfer on your iPhone/iPad/Mac

## How It Works

### Discovery Phase
1. **BLE Advertisement**: Broadcasts presence to wake nearby Apple devices
2. **mDNS Service**: Advertises `_airdrop._tcp` service with device metadata
3. **Device Detection**: Listens for Apple devices advertising the same service

### Connection Phase
1. **Transport Selection**: Attempts Wi-Fi Direct first, falls back to standard Wi-Fi
2. **TLS Handshake**: Establishes secure connection with mutual authentication
3. **Identity Exchange**: Shares device information and capabilities

### Transfer Phase
1. **Discovery Request** (POST /Discover): Initial handshake and identity verification
2. **Ask Request** (POST /Ask): Request permission with file metadata
3. **Upload Request** (POST /Upload): Transfer files via HTTP/2 multipart

## Known Limitations

1. **Manual AirDrop Activation**: Users must manually open AirDrop on Apple devices due to AWDL protocol limitations
2. **Certificate Warnings**: Apple devices will show security warnings for non-Apple-signed certificates
3. **No Contact Matching**: Cannot validate against iCloud contacts (simplified identity system)
4. **Performance**: Wi-Fi Direct may be slower than native AWDL
5. **Discovery Reliability**: Apple devices may not always respond immediately to Windows devices

## Development Status

- [x] Phase 1: Foundation & Research
  - [x] Solution structure
  - [x] Avalonia UI setup
  - [x] Core models and interfaces
  - [x] NuGet packages
- [ ] Phase 2: Network Discovery
- [ ] Phase 3: Network Transport
- [ ] Phase 4: Security & Authentication
- [ ] Phase 5: File Transfer Protocol
- [ ] Phase 6: User Interface
- [ ] Phase 7: Integration & Testing
- [ ] Phase 8: Polish & Deployment

## Contributing

This is currently a work-in-progress. Contributions, suggestions, and bug reports are welcome!

## Legal Notice

This project is an independent implementation of the AirDrop protocol through reverse engineering and publicly available research. It is not affiliated with, endorsed by, or supported by Apple Inc. "AirDrop" is a trademark of Apple Inc.

The implementation is for educational and interoperability purposes. Users should be aware of:
- Potential protocol changes by Apple
- Security implications of accepting files from unknown sources
- Compatibility limitations with official AirDrop implementations

## References

- [OpenDrop](https://github.com/seemoo-lab/opendrop) - Python AirDrop implementation
- [Open Wireless Link](https://owlink.org/) - AWDL protocol research
- [Apple AirDrop Security](https://support.apple.com/guide/security/airdrop-security-sec2261183f4/web)
- [Microsoft Wi-Fi Direct Documentation](https://learn.microsoft.com/en-us/windows/win32/nativewifi/about-the-wi-fi-direct-api)

## License

[To be determined]

## Acknowledgments

- OpenDrop project for protocol research
- Open Wireless Link project for AWDL documentation
- Avalonia UI team for the excellent cross-platform framework
