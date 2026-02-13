---
title: "Phase 5: File Transfer Protocol Implementation"
labels: enhancement, phase-5, protocol
assignees: ""
---

## Phase 5: File Transfer Protocol

Implement HTTP/2 AirDrop protocol with /Discover, /Ask, and /Upload endpoints.

### Objectives
- Implement HTTP/2 server using Kestrel
- Implement AirDrop REST endpoints
- Handle file streaming with progress tracking
- Support multi-file transfers with archive creation

### Tasks

#### HTTP/2 Server Setup
- [ ] Implement `AirDropHttpServer` class
  - Configure Kestrel for HTTP/2
  - Bind to network transport connections
  - Configure TLS with mutual authentication
  - Handle server lifecycle (start/stop)
- [ ] Implement middleware pipeline
  - Request logging middleware
  - Authentication middleware
  - Error handling middleware
  - CORS configuration (if needed)

#### AirDrop Endpoints
- [ ] Implement `/Discover` endpoint (`DiscoverEndpoint.cs`)
  - POST endpoint for initial handshake
  - Exchange device identities
  - Return device capabilities and status
  - Log discovery attempts
- [ ] Implement `/Ask` endpoint (`AskEndpoint.cs`)
  - POST endpoint for transfer permission request
  - Parse file metadata from request
  - Show approval UI to user
  - Return approval/rejection response
- [ ] Implement `/Upload` endpoint (`UploadEndpoint.cs`)
  - POST endpoint for file transfer
  - Handle multipart form data
  - Stream files to disk with progress tracking
  - Extract archive contents (if multiple files)
  - Return transfer status

#### HTTP/2 Client
- [ ] Implement `AirDropHttpClient` class
  - Create HTTP/2 client with TLS
  - Send requests to discovered devices
  - Handle connection pooling
  - Implement retry logic with exponential backoff
- [ ] Implement client request methods
  - `SendDiscoverRequestAsync()` - initial handshake
  - `SendAskRequestAsync()` - request permission
  - `SendUploadRequestAsync()` - upload files

#### File Streaming & Progress
- [ ] Implement `FileStreamHandler` class
  - Stream files with configurable buffer size
  - Track bytes transferred
  - Calculate transfer rate
  - Estimate time remaining
- [ ] Implement `ProgressTracker` class
  - Aggregate progress across multiple files
  - Raise progress events at regular intervals
  - Handle pause/resume (if supported)
  - Compute overall completion percentage

#### Archive Management
- [ ] Implement `ArchiveManager` class
  - Create ZIP archives for multiple files
  - Preserve file metadata (timestamps, permissions)
  - Extract received archives
  - Handle BOM (Bill of Materials) for file paths

#### Transfer Service
- [ ] Implement `TransferService` implementing `ITransferService`
  - Coordinate entire transfer workflow
  - Manage active transfer requests
  - Handle user approval/rejection
  - Monitor transfer progress
  - Clean up temporary files

### Files to Create
- `src/AirDropWindows.Protocol/Http/AirDropHttpServer.cs`
- `src/AirDropWindows.Protocol/Http/AirDropHttpClient.cs`
- `src/AirDropWindows.Protocol/Http/Endpoints/DiscoverEndpoint.cs`
- `src/AirDropWindows.Protocol/Http/Endpoints/AskEndpoint.cs`
- `src/AirDropWindows.Protocol/Http/Endpoints/UploadEndpoint.cs`
- `src/AirDropWindows.Protocol/Http/Models/DiscoverRequest.cs`
- `src/AirDropWindows.Protocol/Http/Models/AskRequest.cs`
- `src/AirDropWindows.Protocol/FileTransfer/FileStreamHandler.cs`
- `src/AirDropWindows.Protocol/FileTransfer/ProgressTracker.cs`
- `src/AirDropWindows.Protocol/FileTransfer/ArchiveManager.cs`
- `src/AirDropWindows.Services/TransferService.cs`

### Acceptance Criteria
- [ ] Successfully send single file Windows → iOS
- [ ] Successfully receive single file iOS → Windows
- [ ] Successfully send multiple files (archive)
- [ ] Progress tracking shows accurate percentages
- [ ] Transfer rate displayed correctly
- [ ] Large file transfers (500MB+) work reliably
- [ ] Graceful handling of transfer cancellation
- [ ] Temporary files cleaned up after transfer

### Performance Targets
- Transfer speed: ≥ 5 MB/s on Wi-Fi Direct
- Memory usage: < 100 MB for large file transfers
- CPU usage: < 25% during active transfer

### Dependencies
- ASP.NET Core Kestrel
- Phase 4 security complete

### Estimated Effort
2 weeks

### References
- [HTTP/2 RFC 7540](https://tools.ietf.org/html/rfc7540)
- [OpenDrop Protocol Implementation](https://github.com/seemoo-lab/opendrop)
- [Kestrel HTTP/2 Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
