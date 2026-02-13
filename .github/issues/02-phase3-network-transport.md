---
title: "Phase 3: Network Transport Implementation"
labels: enhancement, phase-3, networking
assignees: ""
---

## Phase 3: Network Transport Layer

Implement Wi-Fi Direct and standard Wi-Fi network transports to replace Apple's AWDL protocol.

### Objectives
- Establish P2P connections using Wi-Fi Direct (primary)
- Fall back to standard Wi-Fi when Wi-Fi Direct unavailable
- Provide unified network abstraction layer

### Tasks

#### Wi-Fi Direct Transport
- [ ] Implement `WiFiDirectTransport` class implementing `INetworkTransport`
  - Check Wi-Fi Direct availability
  - Create Wi-Fi Direct advertisement publisher
  - Connect to discovered Wi-Fi Direct devices
  - Accept incoming Wi-Fi Direct connections
  - Get local IP address and port
- [ ] Implement connection state machine
  - States: Disconnected → Discovering → Connecting → Connected → Error
  - Handle state transitions with proper event notifications
  - Implement reconnection logic with exponential backoff
- [ ] Implement connection pooling
  - Maintain active connections
  - Reuse existing connections when possible
  - Clean up stale connections

#### Standard Wi-Fi Fallback
- [ ] Implement `StandardWiFiTransport` class implementing `INetworkTransport`
  - Detect same-network devices (via IP address)
  - Direct TCP/IP connections on standard Wi-Fi
  - Get local network interface information
- [ ] Implement network interface selection
  - Choose appropriate network adapter
  - Handle multiple network interfaces
  - Prefer Wi-Fi over Ethernet when both available

#### Connection Manager
- [ ] Implement `ConnectionManager` class
  - Try Wi-Fi Direct first, fall back to standard Wi-Fi
  - Maintain registry of active connections
  - Monitor connection health with keep-alive
  - Handle connection timeouts and errors
- [ ] Implement transport capability negotiation
  - Exchange supported transport types via mDNS TXT records
  - Select best available transport for each device
  - Handle transport switching during connection

### Files to Create
- `src/AirDropWindows.Network/Transports/WiFiDirectTransport.cs`
- `src/AirDropWindows.Network/Transports/StandardWiFiTransport.cs`
- `src/AirDropWindows.Network/ConnectionManager.cs`
- `src/AirDropWindows.Network/ConnectionStateMachine.cs`
- `src/AirDropWindows.Network/NetworkInterfaceHelper.cs`

### Acceptance Criteria
- [ ] Successfully connect to iOS/Mac devices via Wi-Fi Direct
- [ ] Fall back to standard Wi-Fi when Wi-Fi Direct fails
- [ ] Maintain stable connections for duration of file transfer
- [ ] Handle network interruptions gracefully
- [ ] Connection speed ≥ 1 MB/s
- [ ] Connection success rate ≥ 80%

### Dependencies
- Windows.Devices.WiFiDirect APIs
- Phase 2 discovery complete

### Estimated Effort
2 weeks

### References
- [Windows Wi-Fi Direct API](https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/use-wifi-direct)
- [Wi-Fi Direct Protocol](https://www.wi-fi.org/discover-wi-fi/wi-fi-direct)
