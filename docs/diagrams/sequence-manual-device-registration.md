# Sequence Diagram — Manual Device Registration

This diagram shows the flow when a user fills in the device details manually
through the **Register Manually** form instead of uploading an image.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant WEBUI as Blazor UI<br/>(ManualRegister.razor)
    participant API as DevicesController<br/>POST /api/devices/manual
    participant DevSvc as DeviceService
    participant DevRepo as DeviceRepository
    participant DB as ApplicationDBContext<br/>(SQLite)

    User->>WEBUI: Fill in SerialNumber, NickName, Location,<br/>ProductName, ModelNumber, VendorName
    User->>WEBUI: Click "Register Device"

    WEBUI->>API: POST /api/devices/manual (JSON body: ManualDeviceRegisterRequest)

    API->>DevSvc: RegisterManualDeviceAsync(request)

    DevSvc->>DevSvc: Validate at least SerialNumber or NickName present
    alt Neither SerialNumber nor NickName provided
        DevSvc-->>API: throw SerialNumberMissingException
        API-->>WEBUI: 400 Bad Request
        WEBUI-->>User: "Provide at least SerialNumber or NickName"
    end

    alt SerialNumber provided
        DevSvc->>DevRepo: SerialExistsAsync(serialNumber)
        DevRepo-->>DevSvc: exists (bool)
        alt Duplicate serial
            DevSvc-->>API: throw DuplicateSerialNumberException
            API-->>WEBUI: 409 Conflict
            WEBUI-->>User: "A device with the same serial number already exists."
        end
    end

    DevSvc->>DB: Vendors.FirstOrDefaultAsync(normalizedVendorName)
    DB-->>DevSvc: existingVendor or null

    alt Vendor already exists
        DevSvc->>DevSvc: Re-use existing Vendor row
    else New vendor
        DevSvc->>DB: Vendors.AddAsync(newVendor)
    end

    DevSvc->>DevSvc: Create Product entity<br/>(ProductName, ModelNumber, VendorId)
    DevSvc->>DevSvc: Create Device entity<br/>(SerialNumber, NickName, Location, ProductId)

    DevSvc->>DB: Products.Add(product)
    DevSvc->>DevRepo: AddAsync(device)
    DevSvc->>DB: SaveChangesAsync()
    DB-->>DevSvc: (persisted)

    DevSvc-->>API: DeviceResponseDTO
    API-->>WEBUI: 200 OK (DeviceResponseDTO JSON)
    WEBUI-->>User: Show registered device details
```

## ManualDeviceRegisterRequest Fields

| Field | Required | Notes |
|-------|----------|-------|
| `SerialNumber` | At least one of Serial or NickName | Checked for duplicates if supplied |
| `NickName` | At least one of Serial or NickName | Human-friendly label |
| `Location` | Optional | Rack unit, room, etc. |
| `ProductName` | Optional | Defaults to `"Unknown Product"` |
| `ModelNumber` | Optional | Defaults to `"Unknown Model"` |
| `VendorName` | Optional | Defaults to `"ManualEntry"`; reuses existing vendor row if name matches |
