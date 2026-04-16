# Sequence Diagram — Image-Based Device Registration

This diagram shows the end-to-end flow when a user uploads a photo of a device
label to register it automatically via barcode/QR-code scanning.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant UI as Blazor UI
    participant API as DevicesController
    participant Svc as DeviceService
    participant Scan as ScanService
    participant Vendor as VendorLookup
    participant Repo as DeviceRepository
    participant DB as SQLite / EF Core

    User->>UI: Select image, click "Register"
    UI->>API: POST /api/devices/register

    API->>API: Validate file not null/empty
    alt File invalid
        API-->>UI: 400 Bad Request
        UI-->>User: "No file uploaded"
    end

    API->>Svc: RegisterDeviceAsync(imageStream)

    Svc->>Scan: ExtractSerialAsync(imageStream)
    Scan-->>Svc: serialNumber

    alt No serial extracted
        Svc-->>API: SerialNumberMissingException
        API-->>UI: 400 Bad Request
        UI-->>User: "Serial could not be extracted"
    end

    Svc->>Repo: SerialExistsAsync(serial)
    Repo-->>Svc: exists (bool)

    alt Duplicate serial
        Svc-->>API: DuplicateSerialNumberException
        API-->>UI: 409 Conflict
        UI-->>User: "Device already exists"
    end

    Svc->>Vendor: GetProductBySerialAsync(serial)
    Vendor-->>Svc: Product + Vendor info

    Svc->>DB: Find existing Vendor by name
    DB-->>Svc: vendor row or null

    alt Vendor exists
        Svc->>Svc: Re-use vendor row
    else New vendor
        Svc->>DB: Add new Vendor
    end

    Svc->>DB: Products.Add(product)
    Svc->>Repo: AddAsync(device)
    Svc->>DB: SaveChangesAsync()
    DB-->>Svc: Saved

    Svc-->>API: DeviceResponseDTO
    API-->>UI: 200 OK
    UI-->>User: Show device details
```

## Error Paths Summary

| Step | Exception | HTTP Status | User Message |
|------|-----------|-------------|--------------|
| File missing/empty | — | 400 | "No file uploaded." |
| No barcode found in image | `BarcodeNotFoundException` | 400 | Barcode error detail |
| Image processing fails | `FileScanningUploadException` | 400 | Upload error detail |
| Serial not extracted | `SerialNumberMissingException` | 400 | "Serial number could not be extracted…" |
| Duplicate serial | `DuplicateSerialNumberException` | 409 | "A device with the same serial number already exists." |
| Unexpected error | `Exception` | 500 | Generic server error |
