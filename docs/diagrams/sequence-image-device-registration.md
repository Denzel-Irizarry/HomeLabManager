# Sequence Diagram — Image-Based Device Registration

This diagram shows the end-to-end flow when a user uploads a photo of a device
label to register it automatically via barcode/QR-code scanning.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant WEBUI as Blazor UI<br/>(RegisterImage.razor)
    participant API as DevicesController<br/>POST /api/devices/register
    participant DevSvc as DeviceService
    participant ScanSvc as ScanService
    participant VendorLookup as VendorLookupInterface
    participant DevRepo as DeviceRepository
    participant DB as ApplicationDBContext<br/>(SQLite)

    User->>WEBUI: Selects image file and clicks "Register"
    WEBUI->>API: POST /api/devices/register (multipart/form-data)

    API->>API: Validate file not null / not empty
    alt File invalid
        API-->>WEBUI: 400 Bad Request "No file uploaded"
        WEBUI-->>User: Show error message
    end

    API->>DevSvc: RegisterDeviceAsync(imageStream)

    DevSvc->>ScanSvc: ExtractSerialAsync(ScanRequest{imageStream})
    ScanSvc-->>DevSvc: serialNumber (string)

    alt Serial not extracted
        DevSvc-->>API: throw SerialNumberMissingException
        API-->>WEBUI: 400 Bad Request
        WEBUI-->>User: "Serial number could not be extracted"
    end

    DevSvc->>DevRepo: SerialExistsAsync(serialNumber)
    DevRepo-->>DevSvc: exists (bool)

    alt Duplicate serial
        DevSvc-->>API: throw DuplicateSerialNumberException
        API-->>WEBUI: 409 Conflict
        WEBUI-->>User: "Device with this serial already exists"
    end

    DevSvc->>VendorLookup: GetProductBySerialAsync(serialNumber)
    VendorLookup-->>DevSvc: Product (with nested Vendor)

    DevSvc->>DB: Vendors.FirstOrDefaultAsync(normalizedName)
    DB-->>DevSvc: existingVendor or null

    alt Vendor already exists
        DevSvc->>DevSvc: Re-use existing Vendor row
    else New vendor
        DevSvc->>DB: Vendors.AddAsync(newVendor)
    end

    DevSvc->>DevSvc: Create Device entity<br/>(Guid, SerialNumber, ProductId, CreatedAtUtc)
    DevSvc->>DB: Products.Add(product)
    DevSvc->>DevRepo: AddAsync(device)
    DevSvc->>DB: SaveChangesAsync()
    DB-->>DevSvc: (persisted)

    DevSvc-->>API: DeviceResponseDTO
    API-->>WEBUI: 200 OK (DeviceResponseDTO JSON)
    WEBUI-->>User: Show success with device details
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
