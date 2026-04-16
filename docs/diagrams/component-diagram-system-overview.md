# Component Diagram — System Overview

This diagram shows the high-level runtime components of HomeLabManager,
how they are organised into projects, and how data flows between them.

```mermaid
flowchart TD
    subgraph BROWSER["🌐  Browser / End User"]
        UI["Blazor Server UI
        HomeLabManager.WEBUI
        ───────────────────
        Dashboard · Devices · Components
        Vendors · Register · Settings"]
    end

    subgraph API["🖥️  HomeLabManager.API"]
        CTRL["Controllers
        ───────────────────
        Devices · Components
        DeviceComponents · Scraper · Vendors"]

        SVC["Services
        ───────────────────
        DeviceService · ComponentService
        DeviceComponentService · ScraperService"]

        REPO["Repositories
        ───────────────────
        DeviceRepository
        ComponentRepository
        DeviceComponentRepository"]

        SCAN["ScanService
        ───────────────────
        Barcode / QR extraction"]

        PIPE["Scraping Providers
        ───────────────────
        UpcLookupProvider
        HpeSerialLookupProvider
        DellSerialLookupProvider
        CiscoSerialLookupProvider
        WebSearchFallbackProvider"]

        DBC[("ApplicationDBContext
        EF Core")]
    end

    subgraph CORE["📦  HomeLabManager.Core  (shared library)"]
        ENT["Domain Entities
        ───────────────────
        Device · Product · Vendor
        Component · DeviceComponent"]

        DTO["Scraping Contracts
        ───────────────────
        ScrapeResult · ScrapedDeviceInfo
        ScrapeRequest"]
    end

    subgraph PERSIST["🗄️  Persistence"]
        DB[("SQLite Database
        homelabmanager.db")]
    end

    subgraph TESTS["🧪  HomeLabManager.API.Tests"]
        UNIT["xUnit Tests
        ───────────────────
        ScraperServiceRoutingTests
        SerialVendorDetectorTests
        DeviceServiceVendorDedupTests"]

        FAKE["Fake Implementations
        ───────────────────
        FakeVendorLookupTest
        FakeHardwareLookupProvider
        FakeHpeSerialLookupProvider
        FakeCiscoSerialLookupProvider"]
    end

    %% ── Runtime data flow ───────────────────────────────────────────────────
    UI      -->|"HTTP REST"| CTRL
    CTRL    --> SVC
    SVC     --> REPO
    SVC     --> SCAN
    SVC     --> PIPE
    REPO    --> DBC
    DBC     --> DB

    %% ── Project references ──────────────────────────────────────────────────
    API     -.->|"ProjectReference"| CORE
    BROWSER -.->|"ProjectReference"| CORE
    TESTS   -.->|"ProjectReference"| API
    TESTS   -.->|"ProjectReference"| CORE

    %% ── Styles ──────────────────────────────────────────────────────────────
    classDef ui       fill:#ede9fe,stroke:#8b5cf6,color:#3b0764
    classDef api      fill:#dbeafe,stroke:#3b82f6,color:#1e3a5f
    classDef core     fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e
    classDef persist  fill:#d1fae5,stroke:#10b981,color:#064e3b
    classDef test     fill:#fef9c3,stroke:#d97706,color:#78350f

    class UI ui
    class CTRL,SVC,REPO,SCAN,PIPE,DBC api
    class ENT,DTO core
    class DB persist
    class UNIT,FAKE test
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
