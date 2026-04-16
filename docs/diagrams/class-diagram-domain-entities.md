# Class Diagram — Core Domain Entities

This diagram shows the five domain entities that live in `HomeLabManager.Core/Entities/`
and how they relate to each other in the database.

```mermaid
classDiagram
    direction LR

    class Device {
        +Guid Id
        +string? SerialNumber
        +string? NickName
        +string? Location
        +Guid ProductId
        +Product? Product
        +DateTime CreatedAtUtc
    }

    class Product {
        +Guid Id
        +string ModelNumber
        +string ProductName
        +int? CPUCount
        +string? CPUName
        +int? Memory
        +int? RamSpeed
        +string? StorageForDevice
        +Guid VendorId
        +Vendor? Vendor
    }

    class Vendor {
        +Guid Id
        +string VendorName
        +string? VendorBaseUrl
    }

    class Component {
        +Guid Id
        +string Name
        +string? ComponentType
        +string? Manufacturer
        +string? ModelNumber
        +string? Specifications
        +decimal? UnitPrice
        +Guid? VendorId
        +DateTime CreatedAtUtc
    }

    class DeviceComponent {
        +Guid Id
        +Guid DeviceId
        +Guid ComponentId
        +string? SerialNumber
        +DateTime? InstalledDate
        +string? Notes
        +DateTime CreatedAtUtc
    }

    %% Relationships
    Device "many" --> "1" Product       : ProductId (FK)
    Product "many" --> "1" Vendor       : VendorId (FK)
    Component "many" --> "0..1" Vendor  : VendorId (nullable FK)
    DeviceComponent "many" --> "1" Device    : DeviceId (FK)
    DeviceComponent "many" --> "1" Component : ComponentId (FK)
```

## Key Points

- **Device** is the central asset being tracked. It always links to a **Product** (model info) and through that to a **Vendor**.
- **Product** holds hardware-specification metadata (CPU, RAM, storage) sourced from a vendor lookup or entered manually.
- **Vendor** is the manufacturer/supplier. The same vendor row is reused (deduplicated) across many products and components.
- **Component** tracks individual parts (CPU, RAM, NIC, etc.) that can be independently purchased and installed.
- **DeviceComponent** is the join table that records *which* component is installed in *which* device, along with installation metadata (serial number, date, notes).
