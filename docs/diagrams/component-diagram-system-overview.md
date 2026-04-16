# Component Diagram — System Overview

This diagram shows the high-level runtime components of HomeLabManager,
how they are organised into projects, and how data flows between them.

```mermaid
graph TB
    subgraph Browser["🌐 Browser / End User"]
        UI[Blazor Server UI\nHomeLabManager.WEBUI]
    end

    subgraph API_Server["🖥️ API Server\nHomeLabManager.API"]
        direction TB
        Controllers["Controllers\nDevices · Components\nDeviceComponents · Scraper · Vendors"]
        Services["Services\nDeviceService · ComponentService\nDeviceComponentService · ScraperService"]
        Repositories["Repositories\nDeviceRepository · ComponentRepository\nDeviceComponentRepository"]
        DBContext["ApplicationDBContext\n(EF Core)"]
        ScanSvc["ScanService\n(barcode / QR extraction)"]
        ScrapingPipeline["Scraping Provider Pipeline\nUpcLookupProvider\nHpeSerialLookupProvider\nDellSerialLookupProvider\nCiscoSerialLookupProvider\nWebSearchFallbackProvider"]
    end

    subgraph Core["📦 Shared Library\nHomeLabManager.Core"]
        Entities["Domain Entities\nDevice · Product · Vendor\nComponent · DeviceComponent"]
        ScrapingContracts["Scraping Contracts\nScrapeResult · ScrapedDeviceInfo\nScrapeRequest"]
    end

    subgraph DB["🗄️ Persistence"]
        SQLite[(SQLite Database\nhomelabmanager.db)]
    end

    subgraph Tests["🧪 Test Project\nHomeLabManager.API.Tests"]
        xUnit["xUnit Tests\nScraperServiceRoutingTests\nSerialVendorDetectorTests\nDeviceServiceVendorDedupTests"]
        FakeImpls["Fake Implementations\nFakeVendorLookupTest\nFakeHardwareLookupProvider\nFakeHpeSerialLookupProvider\nFakeCiscoSerialLookupProvider\nFakeSerialLookupProvider"]
    end

    %% Data flow
    UI -->|"HTTP REST (HttpClient)"| Controllers
    Controllers --> Services
    Services --> Repositories
    Services --> ScanSvc
    Services --> ScrapingPipeline
    Repositories --> DBContext
    DBContext --> SQLite

    %% Shared library references
    API_Server -.->|"ProjectReference"| Core
    UI -.->|"ProjectReference"| Core
    Tests -.->|"ProjectReference"| API_Server
    Tests -.->|"ProjectReference"| Core

    %% Style
    classDef project fill:#dbeafe,stroke:#3b82f6,color:#1e3a5f
    classDef db fill:#d1fae5,stroke:#10b981,color:#064e3b
    classDef test fill:#fef9c3,stroke:#eab308,color:#713f12
    classDef browser fill:#ede9fe,stroke:#8b5cf6,color:#3b0764

    class UI browser
    class Controllers,Services,Repositories,DBContext,ScanSvc,ScrapingPipeline project
    class Entities,ScrapingContracts project
    class SQLite db
    class xUnit,FakeImpls test
```

## Component Responsibilities

| Component | Project | Responsibility |
|-----------|---------|----------------|
| **Blazor UI** | WEBUI | Renders pages (Dashboard, Devices, Components, Vendors, Register, Settings). Communicates with the API via `HttpClient`. |
| **Controllers** | API | Thin HTTP boundary — validates HTTP input, delegates to services, maps results to HTTP responses. |
| **Services** | API | Business logic — orchestration, validation rules, cross-entity consistency. |
| **Repositories** | API | Data access — EF Core queries isolated behind interfaces for testability. |
| **ScanService** | API | Decodes barcodes/QR codes from uploaded images using a scanning library. |
| **Scraping Pipeline** | API | Chain of `IHardwareLookupProvider` implementations tried in order (UPC database → vendor-specific serial APIs → web-search fallback). |
| **ApplicationDBContext** | API | EF Core DbContext; owns the schema migration and table mapping. |
| **Core Entities** | Core | Plain C# POCO domain models shared by API and WEBUI with no external dependencies. |
| **Scraping Contracts** | Core | DTOs and result objects for the scraping pipeline, also shared across projects. |
| **SQLite DB** | — | Single-file embedded database; ideal for home-lab / single-node deployment. |
