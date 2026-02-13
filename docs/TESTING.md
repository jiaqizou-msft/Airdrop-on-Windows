# AirDrop Windows - Testing Guide

## Overview

This document provides comprehensive testing procedures for the AirDrop Windows application.

## Testing Phases

### Phase 1: Unit Testing

#### Core Components
- **Configuration Service**
  - Test configuration load/save
  - Test default settings generation
  - Test settings validation

- **Security Service**
  - Test certificate generation
  - Test certificate renewal
  - Test encryption/decryption
  - Test identity record generation

- **Discovery Service**
  - Test BLE advertisement scanning
  - Test mDNS service browsing
  - Test device registry
  - Test device expiration

- **Transfer Service**
  - Test file metadata creation
  - Test transfer request management
  - Test approval/rejection workflow

#### Running Unit Tests
```bash
cd src
dotnet test --configuration Release
```

### Phase 2: Integration Testing

#### Discovery Integration
1. **BLE + mDNS Integration**
   - Start discovery service
   - Verify BLE scanner starts
   - Verify mDNS service publisher starts
   - Check for proper event coordination

2. **Network Transport Selection**
   - Test Wi-Fi Direct connection establishment
   - Test fallback to Standard Wi-Fi
   - Verify connection pooling

3. **Security Integration**
   - Test TLS handshake with self-signed certificates
   - Verify mutual authentication
   - Test certificate validation

#### Running Integration Tests
```bash
cd src
dotnet test --filter Category=Integration
```

### Phase 3: End-to-End Testing

#### Prerequisites
- Windows PC with Wi-Fi and Bluetooth
- iOS device (iPhone/iPad) with AirDrop enabled
- macOS device (optional) with AirDrop enabled
- Both devices on same Wi-Fi network or in Bluetooth range

#### Test Scenarios

**Scenario 1: Windows → iOS File Transfer**
1. Start AirDrop Windows application
2. On iPhone, open Control Center → Enable AirDrop → Set to "Everyone"
3. Click "Start Discovery" in Windows app
4. Verify iPhone appears in device list within 30 seconds
5. Click "Send Files" button next to iPhone
6. Select test file (< 10MB recommended)
7. Accept transfer on iPhone
8. Verify file received correctly on iPhone
9. Check file integrity (size, content, timestamp)

**Scenario 2: iOS → Windows File Transfer**
1. Start AirDrop Windows application
2. Click "Start Discovery"
3. On iPhone, open any file (Photo, Document)
4. Tap Share → AirDrop
5. Verify Windows PC appears in AirDrop list
6. Tap Windows PC name
7. Verify transfer request appears in Windows app
8. Click "Accept" in Windows app
9. Verify file saved to configured location
10. Check file integrity

**Scenario 3: Multiple File Transfer**
1. Start discovery
2. Select 3-5 files of varying sizes
3. Send to discovered iOS device
4. Verify progress tracking shows correctly
5. Verify all files received
6. Check that files are not corrupted

**Scenario 4: Large File Transfer (> 100MB)**
1. Start discovery
2. Select a file larger than 100MB
3. Initiate transfer
4. Monitor progress bar
5. Verify transfer rate is reasonable (> 1MB/s)
6. Verify transfer completes successfully
7. Verify no file corruption

**Scenario 5: Transfer Cancellation**
1. Start a large file transfer
2. Click "Cancel" during transfer
3. Verify transfer stops
4. Verify no partial files remain
5. Verify app returns to ready state

**Scenario 6: Network Interruption**
1. Start file transfer
2. Disable Wi-Fi adapter mid-transfer
3. Verify graceful error handling
4. Re-enable Wi-Fi
5. Retry transfer
6. Verify successful completion

**Scenario 7: Multiple Concurrent Transfers**
1. Configure max concurrent transfers to 3
2. Initiate transfers to 3 different devices
3. Verify all transfers proceed
4. Verify progress tracking for each
5. Verify all complete successfully

### Phase 4: Performance Testing

#### Discovery Performance
- **Metric**: Time to discover device
- **Target**: < 30 seconds
- **Test**: Start discovery, measure time until first device appears

#### Transfer Performance
- **Metric**: Transfer throughput
- **Target**: ≥ 1 MB/s on Wi-Fi Direct
- **Test**: Transfer 100MB file, calculate throughput

#### Memory Usage
- **Metric**: Memory consumption during idle and transfer
- **Target**: < 200MB idle, < 500MB during transfer
- **Test**: Monitor Task Manager during operations

#### CPU Usage
- **Metric**: CPU utilization
- **Target**: < 5% idle, < 25% during transfer
- **Test**: Monitor Task Manager CPU percentage

### Phase 5: Security Testing

#### Certificate Validation
- Verify self-signed certificate generation
- Test certificate storage in Windows Certificate Store
- Verify certificate renewal
- Test expired certificate handling

#### TLS Mutual Authentication
- Verify both client and server certificates are validated
- Test connection failure with invalid certificates
- Verify encryption is used for all transfers

#### Input Validation
- Test filename sanitization (prevent directory traversal)
- Test file size limits
- Test invalid file types
- Test malformed protocol messages

### Phase 6: Compatibility Testing

#### Windows Versions
- Windows 11 (primary target)
- Windows 10 (19041+)

#### Apple Devices
- iPhone (iOS 14+)
- iPad (iPadOS 14+)
- MacBook (macOS Big Sur+)
- iMac (macOS Big Sur+)

#### Network Scenarios
- Same Wi-Fi network
- Different Wi-Fi networks (should fail gracefully)
- Wi-Fi Direct only
- Bluetooth discovery with Wi-Fi transfer

## Test Results Template

### Test Execution Log

```
Test Date: ____________________
Tester: ____________________
Environment:
- Windows Version: ____________________
- iOS Device: ____________________
- Network: ____________________

Results:
┌─────────────────────────┬─────────┬──────────┐
│ Test Scenario           │ Result  │ Notes    │
├─────────────────────────┼─────────┼──────────┤
│ Windows → iOS Transfer  │ PASS    │          │
│ iOS → Windows Transfer  │ PASS    │          │
│ Multiple File Transfer  │ PASS    │          │
│ Large File Transfer     │ PASS    │          │
│ Transfer Cancellation   │ PASS    │          │
│ Network Interruption    │ FAIL    │ See #123 │
│ Concurrent Transfers    │ PASS    │          │
└─────────────────────────┴─────────┴──────────┘

Issues Found:
1. [Issue description]
2. [Issue description]
```

## Known Limitations

1. **Manual AirDrop Activation**: Users must manually open AirDrop on Apple devices before discovery
2. **Certificate Warnings**: Apple devices show security warning on first transfer (expected)
3. **No Contact Matching**: Cannot validate against iCloud contacts
4. **Performance**: Wi-Fi Direct may be slower than native AWDL
5. **Discovery Time**: Initial discovery may take 10-30 seconds

## Success Criteria

- ✅ Device discovery success rate ≥ 90%
- ✅ Transfer success rate ≥ 80%
- ✅ Transfer speed ≥ 1 MB/s
- ✅ No crashes during normal operation
- ✅ Graceful error handling for all failure scenarios

## Reporting Issues

When reporting issues, include:
1. Windows version and build number
2. Apple device model and iOS/macOS version
3. Network configuration (Wi-Fi, Bluetooth)
4. Steps to reproduce
5. Expected vs actual behavior
6. Log files from `%LOCALAPPDATA%\AirDropWindows\Logs\`

## Automated Testing (Future)

Future enhancements:
- Implement xUnit/NUnit test framework
- Create mock services for unit testing
- Develop integration test suite
- Set up CI/CD with automated testing
- Performance benchmarking automation
