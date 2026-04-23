# Sequence Diagram — Manual Device Registration

This diagram shows the flow when a user fills in the device details manually
through the **Register Manually** form instead of uploading an image.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant UI as Blazor ManualRegister Page
    participant API as DevicesController POST /api/devices/manual
    participant Svc as DeviceService
    participant Repo as DeviceRepository
    participant DB as ApplicationDBContext / SQLite

    User->>UI: Fill in SerialNumber, NickName, Location, ProductName, ModelNumber, VendorName
    User->>UI: Click Register Device button

    UI->>API: POST /api/devices/manual (JSON body: ManualDeviceRegisterRequest)

    API->>Svc: RegisterManualDeviceAsync(request)

    Svc->>Svc: Validate that at least SerialNumber or NickName is provided
    alt Neither SerialNumber nor NickName was provided
        Svc-->>API: Throw SerialNumberMissingException
        API-->>UI: 400 Bad Request
        UI-->>User: Please provide at least a SerialNumber or NickName
    end

    alt SerialNumber was provided
        Svc->>Repo: SerialExistsAsync(serialNumber)
        Repo-->>Svc: exists boolean
        alt Duplicate serial number detected
            Svc-->>API: Throw DuplicateSerialNumberException
            API-->>UI: 409 Conflict
            UI-->>User: A device with the same serial number already exists
        end
    end

    Svc->>DB: Vendors.FirstOrDefaultAsync by normalized vendor name
    DB-->>Svc: Existing vendor row or null

    alt Vendor already exists in database
        Svc->>Svc: Re-use existing Vendor row
    else New vendor name
        Svc->>DB: Vendors.AddAsync with new Vendor
    end

    Svc->>Svc: Create Product entity (ProductName, ModelNumber, VendorId)
    Svc->>Svc: Create Device entity (SerialNumber, NickName, Location, ProductId)

    Svc->>DB: Products.Add(product)
    Svc->>Repo: AddAsync(device)
    Svc->>DB: SaveChangesAsync()
    DB-->>Svc: Changes persisted successfully

    Svc-->>API: DeviceResponseDTO
    API-->>UI: 200 OK with DeviceResponseDTO JSON
    UI-->>User: Show registered device details
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
