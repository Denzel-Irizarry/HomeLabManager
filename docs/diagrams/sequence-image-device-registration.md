# Sequence Diagram — Image-Based Device Registration

This diagram shows the end-to-end flow when a user uploads a photo of a device
label to register it automatically via barcode/QR-code scanning.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant UI as Blazor RegisterImage Page
    participant API as DevicesController POST /api/devices/register
    participant Svc as DeviceService
    participant Scan as ScanService (barcode decoder)
    participant Vendor as VendorLookupInterface
    participant Repo as DeviceRepository
    participant DB as ApplicationDBContext / SQLite

    User->>UI: Select device image file and click Register
    UI->>API: POST /api/devices/register (multipart form-data)

    API->>API: Validate file is not null and not empty
    alt File is missing or empty
        API-->>UI: 400 Bad Request — No file uploaded
        UI-->>User: Display error message
    end

    API->>Svc: RegisterDeviceAsync(imageStream)

    Svc->>Scan: ExtractSerialAsync(ScanRequest with imageStream)
    Scan-->>Svc: Extracted serialNumber string

    alt Serial number could not be extracted
        Svc-->>API: Throw SerialNumberMissingException
        API-->>UI: 400 Bad Request
        UI-->>User: Serial number could not be extracted from image
    end

    Svc->>Repo: SerialExistsAsync(serialNumber)
    Repo-->>Svc: exists boolean

    alt Serial number already registered
        Svc-->>API: Throw DuplicateSerialNumberException
        API-->>UI: 409 Conflict
        UI-->>User: A device with this serial number already exists
    end

    Svc->>Vendor: GetProductBySerialAsync(serialNumber)
    Vendor-->>Svc: Product with nested Vendor object

    Svc->>DB: Vendors.FirstOrDefaultAsync by normalized name
    DB-->>Svc: Existing vendor row or null

    alt Vendor already exists in database
        Svc->>Svc: Re-use existing Vendor row
    else New vendor
        Svc->>DB: Vendors.AddAsync with new Vendor
    end

    Svc->>DB: Products.Add(product)
    Svc->>Repo: AddAsync(device)
    Svc->>DB: SaveChangesAsync()
    DB-->>Svc: Changes persisted successfully

    Svc-->>API: DeviceResponseDTO
    API-->>UI: 200 OK with DeviceResponseDTO JSON
    UI-->>User: Show success message with device details
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
