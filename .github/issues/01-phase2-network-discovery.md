---
title: "Phase 2: Network Discovery Implementation"
labels: enhancement, phase-2, discovery
assignees: ""
---

## Phase 2: Network Discovery

Implement device discovery using BLE advertisements and mDNS/Bonjour service discovery.

### Objectives
- Discover nearby AirDrop-compatible devices
- Advertise this Windows device to Apple devices
- Maintain a registry of discovered devices with timeout handling

### Tasks

#### BLE Advertisement Layer
- [ ] Implement `BleAdvertisementScanner` class
  - Scan for AirDrop BLE advertisements (UUID: `0000af0a-0000-1000-8000-00805f9b34fb`)
  - Parse advertisement data to extract device information
  - Handle advertisement received events
- [ ] Implement `BleAdvertisementPublisher` class
  - Broadcast BLE advertisements for this device
  - Include device metadata in advertisement payload
  - Handle start/stop advertisement lifecycle

#### mDNS Service Discovery
- [ ] Implement `MdnsServiceBrowser` class
  - Browse for `_airdrop._tcp` services
  - Parse service TXT records for device capabilities
  - Handle service discovered/removed events
- [ ] Implement `MdnsServicePublisher` class
  - Publish `_airdrop._tcp` service for this device
  - Include TXT records with device metadata
  - Handle dynamic IP address changes

#### Device Registry
- [ ] Implement `DeviceRegistry` class
  - Maintain list of discovered devices
  - Update device last-seen timestamps
  - Remove expired devices (configurable timeout)
  - Merge BLE and mDNS discovery data
- [ ] Implement `DiscoveryService` implementing `IDiscoveryService`
  - Coordinate BLE and mDNS discovery
  - Raise device discovered/lost/updated events
  - Handle start/stop lifecycle

### Files to Create
- `src/AirDropWindows.Discovery/BLE/BleAdvertisementScanner.cs`
- `src/AirDropWindows.Discovery/BLE/BleAdvertisementPublisher.cs`
- `src/AirDropWindows.Discovery/mDNS/MdnsServiceBrowser.cs`
- `src/AirDropWindows.Discovery/mDNS/MdnsServicePublisher.cs`
- `src/AirDropWindows.Discovery/DeviceRegistry.cs`
- `src/AirDropWindows.Discovery/DiscoveryService.cs`

### Acceptance Criteria
- [ ] Windows device appears in AirDrop device list on iOS/Mac
- [ ] iOS/Mac devices appear in Windows app discovery list within 30 seconds
- [ ] Device list updates when devices come and go
- [ ] No crashes or memory leaks during discovery
- [ ] Logs show clear discovery activity

### Dependencies
- Windows.Devices.Bluetooth APIs
- Makaretu.Dns.Multicast library
- Phase 1 foundation complete ✅

### Estimated Effort
2 weeks

### References
- [Windows Bluetooth LE Advertisement API](https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/ble-beacon)
- [mDNS/DNS-SD RFC 6763](https://tools.ietf.org/html/rfc6763)
- [OpenDrop Discovery Implementation](https://github.com/seemoo-lab/opendrop)
