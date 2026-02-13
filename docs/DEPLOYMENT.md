# AirDrop Windows - Deployment Guide

## Overview

This guide covers packaging, distribution, and deployment of AirDrop Windows application.

## Prerequisites

- Windows 11 SDK (10.0.19041.0 or later)
- .NET 10.0 SDK
- Visual Studio 2022 or JetBrains Rider
- Windows App Certification Kit (optional, for store submission)
- Code signing certificate (for production releases)

## Build Configurations

### Development Build
```bash
cd src
dotnet build --configuration Debug
```

### Release Build
```bash
cd src
dotnet build --configuration Release
```

### Self-Contained Build
```bash
cd src/AirDropWindows.UI
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true
```

## Packaging Options

### Option 1: MSIX Package (Recommended)

MSIX is the modern Windows packaging format with automatic updates and clean uninstall.

#### Step 1: Create MSIX Project

1. Add Windows Application Packaging Project to solution
2. Set AirDropWindows.UI as entry point
3. Configure Package.appxmanifest

#### Step 2: Configure Manifest

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">

  <Identity Name="AirDropWindows"
            Publisher="CN=Your Name"
            Version="0.1.0.0" />

  <Properties>
    <DisplayName>AirDrop for Windows</DisplayName>
    <PublisherDisplayName>Your Name</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.22631.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-us" />
  </Resources>

  <Applications>
    <Application Id="AirDropWindows" Executable="AirDropWindows.UI.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="AirDrop for Windows"
                          Description="Share files with nearby Apple devices"
                          BackgroundColor="transparent"
                          Square150x150Logo="Assets\Square150x150Logo.png"
                          Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <Capability Name="internetClient" />
    <Capability Name="privateNetworkClientServer" />
    <DeviceCapability Name="bluetooth" />
    <DeviceCapability Name="wiFiControl" />
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
```

#### Step 3: Build MSIX

```bash
msbuild AirDropWindows.Packaging.wapproj /p:Configuration=Release /p:Platform=x64
```

#### Step 4: Sign Package

```bash
signtool sign /fd SHA256 /f YourCertificate.pfx /p YourPassword AirDropWindows_0.1.0.0_x64.msix
```

### Option 2: Installer (WiX Toolset)

For traditional installer experience.

#### Install WiX Toolset
```bash
dotnet tool install --global wix
```

#### Create Product.wxs
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="AirDrop for Windows" Language="1033" Version="0.1.0.0"
           Manufacturer="Your Name" UpgradeCode="PUT-GUID-HERE">
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />

    <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
    <MediaTemplate EmbedCab="yes" />

    <Feature Id="ProductFeature" Title="AirDrop for Windows" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
    </Feature>
  </Product>

  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="AirDrop for Windows" />
      </Directory>
    </Directory>
  </Fragment>

  <Fragment>
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <Component Id="MainExecutable">
        <File Source="$(var.PublishDir)AirDropWindows.UI.exe" />
      </Component>
      <!-- Add all other files -->
    </ComponentGroup>
  </Fragment>
</Wix>
```

#### Build Installer
```bash
wix build Product.wxs -o AirDropWindows-Setup.msi
```

### Option 3: Portable ZIP

For users who prefer portable applications.

```bash
cd src/AirDropWindows.UI
dotnet publish -c Release -r win-x64 --self-contained true
cd bin/Release/net10.0-windows10.0.19041.0/win-x64/publish
7z a AirDropWindows-Portable.zip *
```

## Code Signing

### Obtain Certificate

**Option A: Self-Signed (Testing Only)**
```powershell
New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=Your Name" -KeyUsage DigitalSignature -FriendlyName "AirDrop Code Signing" -CertStoreLocation "Cert:\CurrentUser\My"
```

**Option B: Commercial Certificate**
- Purchase from DigiCert, Sectigo, or similar CA
- Verify your organization identity
- Install certificate in Windows Certificate Store

### Sign Executables

```bash
# Sign main executable
signtool sign /fd SHA256 /sha1 CERT_THUMBPRINT /t http://timestamp.digicert.com AirDropWindows.UI.exe

# Sign all DLLs
Get-ChildItem -Recurse -Filter *.dll | ForEach-Object {
    signtool sign /fd SHA256 /sha1 CERT_THUMBPRINT /t http://timestamp.digicert.com $_.FullName
}
```

## Distribution Methods

### Method 1: GitHub Releases

1. Create release tag
```bash
git tag -a v0.1.0 -m "Initial release"
git push origin v0.1.0
```

2. Create GitHub Release
```bash
gh release create v0.1.0 --title "AirDrop Windows v0.1.0" --notes "Release notes here"
```

3. Upload artifacts
```bash
gh release upload v0.1.0 AirDropWindows_0.1.0_x64.msix
gh release upload v0.1.0 AirDropWindows-Portable.zip
```

### Method 2: Microsoft Store

1. Create Partner Center account
2. Reserve app name: "AirDrop for Windows"
3. Upload MSIX package
4. Complete store listing:
   - Description
   - Screenshots
   - System requirements
   - Privacy policy
5. Submit for certification

### Method 3: Direct Download

Host on your website with proper security:
- HTTPS only
- SHA256 checksums provided
- Digital signature verification instructions
- Clear installation guide

## System Requirements

### Minimum Requirements
- **OS**: Windows 10 (Build 19041) or Windows 11
- **RAM**: 4 GB
- **Storage**: 100 MB free space
- **Network**: Wi-Fi adapter with Wi-Fi Direct support
- **Bluetooth**: Bluetooth 4.0+ (for discovery)

### Recommended Requirements
- **OS**: Windows 11 (latest)
- **RAM**: 8 GB+
- **Network**: 802.11ac Wi-Fi adapter
- **Bluetooth**: Bluetooth 5.0+

## Installation Instructions

### MSIX Package Installation

**For Users:**
1. Download AirDropWindows_0.1.0_x64.msix
2. Double-click to install
3. Click "Install" when prompted
4. Launch from Start Menu

**For Developers:**
```powershell
Add-AppxPackage -Path AirDropWindows_0.1.0_x64.msix
```

### MSI Installer

1. Download AirDropWindows-Setup.msi
2. Run installer
3. Follow installation wizard
4. Launch from Start Menu or Desktop shortcut

### Portable Version

1. Download AirDropWindows-Portable.zip
2. Extract to desired location
3. Run AirDropWindows.UI.exe

## Configuration

### First-Time Setup

1. Launch application
2. Allow Windows Firewall exceptions
3. Allow Bluetooth and Wi-Fi permissions
4. Configure settings:
   - Discovery mode (Everyone/Contacts Only)
   - Default save location
   - Auto-accept transfers (optional)

### Advanced Configuration

Edit `%APPDATA%\AirDropWindows\config.json`:

```json
{
  "device": {
    "discoveryMode": "Everyone",
    "autoAccept": false,
    "defaultSaveLocation": "C:\\Users\\YourName\\Documents\\AirDrop"
  },
  "network": {
    "useWiFiDirect": true,
    "port": 8771,
    "connectionTimeoutSeconds": 30
  },
  "security": {
    "requireApproval": true,
    "acceptSelfSignedCertificates": true
  }
}
```

## Firewall Configuration

### Automatic (Recommended)

Application will request firewall permissions on first run.

### Manual

```powershell
New-NetFirewallRule -DisplayName "AirDrop Windows" -Direction Inbound -Program "C:\Program Files\AirDrop for Windows\AirDropWindows.UI.exe" -Action Allow
```

## Troubleshooting Deployment

### Issue: MSIX Installation Fails

**Solution:**
- Verify Windows version (19041+)
- Check digital signature
- Enable Developer Mode temporarily
- Use `Add-AppxPackage -Path package.msix -ErrorAction Stop` for detailed errors

### Issue: Code Signing Errors

**Solution:**
- Verify certificate is in correct store
- Check certificate expiration
- Ensure timestamp server is accessible
- Use SHA256 algorithm (SHA1 deprecated)

### Issue: Store Certification Fails

**Common Reasons:**
- Missing capabilities in manifest
- Unsigned binaries
- Privacy policy URL not working
- Screenshots don't meet requirements

## Updates

### Automatic Updates (MSIX)

MSIX packages support automatic updates:
1. Increment version in Package.appxmanifest
2. Build new package
3. Upload to distribution channel
4. Users receive update automatically

### Manual Updates

For portable/installer versions:
- Implement update checker in application
- Compare current version with latest GitHub release
- Download and install new version

## Uninstallation

### MSIX
```powershell
Remove-AppxPackage -Package AirDropWindows_0.1.0.0_x64__8wekyb3d8bbwe
```

### MSI
```
Settings → Apps → AirDrop for Windows → Uninstall
```

### Portable
Simply delete the folder.

## Post-Deployment

### Monitoring

- Set up crash reporting (e.g., Sentry)
- Monitor GitHub Issues
- Track download statistics
- Collect user feedback

### Marketing

- Create product website
- Write blog post announcement
- Share on social media
- Submit to software directories (e.g., AlternativeTo, Softpedia)

## Security Considerations

1. **Code Signing**: Always sign production releases
2. **HTTPS**: Distribute only via HTTPS
3. **Checksums**: Provide SHA256 checksums
4. **Auto-Update**: Implement secure update mechanism
5. **Privacy**: Include privacy policy
6. **Permissions**: Request only necessary capabilities

## Release Checklist

- [ ] Update version numbers
- [ ] Update CHANGELOG.md
- [ ] Run full test suite
- [ ] Build release packages
- [ ] Sign all binaries
- [ ] Generate checksums
- [ ] Create release notes
- [ ] Tag release in Git
- [ ] Upload to distribution channels
- [ ] Update website/documentation
- [ ] Announce release

## Support

For deployment issues:
- GitHub Issues: https://github.com/jiaqizou-msft/Airdrop-on-Windows/issues
- Email: [Your email]
- Documentation: https://github.com/jiaqizou-msft/Airdrop-on-Windows/wiki
