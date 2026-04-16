# Sequence Diagram — Manual Device Registration

This diagram shows the flow when a user fills in the device details manually
through the **Register Manually** form instead of uploading an image.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant UI as Blazor UI
    participant API as DevicesController
    participant Svc as DeviceService
    participant Repo as DeviceRepository
    participant DB as SQLite / EF Core

    User->>UI: Fill form (Serial, NickName, Location, Product, Vendor)
    User->>UI: Click "Register Device"

    UI->>API: POST /api/devices/manual

    API->>Svc: RegisterManualDeviceAsync(request)

    Svc->>Svc: Validate Serial or NickName present
    alt Neither provided
        Svc-->>API: SerialNumberMissingException
        API-->>UI: 400 Bad Request
        UI-->>User: "Provide at least Serial or NickName"
    end

    alt Serial provided
        Svc->>Repo: SerialExistsAsync(serial)
        Repo-->>Svc: exists (bool)
        alt Duplicate serial
            Svc-->>API: DuplicateSerialNumberException
            API-->>UI: 409 Conflict
            UI-->>User: "Device already exists"
        end
    end

    Svc->>DB: Find Vendor by normalized name
    DB-->>Svc: vendor row or null

    alt Vendor exists
        Svc->>Svc: Re-use vendor row
    else New vendor
        Svc->>DB: Add new Vendor
    end

    Svc->>Svc: Create Product entity
    Svc->>Svc: Create Device entity

    Svc->>DB: Products.Add(product)
    Svc->>Repo: AddAsync(device)
    Svc->>DB: SaveChangesAsync()
    DB-->>Svc: Saved

    Svc-->>API: DeviceResponseDTO
    API-->>UI: 200 OK
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
