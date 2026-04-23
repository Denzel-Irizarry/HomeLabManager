# Sequence Diagram — Hardware Scraping / Lookup Pipeline

This diagram shows how the scraper feature works — either from a text query
(serial number or UPC) entered on the Scraper page, or from an uploaded image.

## Path A — Text Search

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant UI as Blazor Scraper Page
    participant API as ScraperController
    participant Svc as ScraperService
    participant Det as SerialVendorDetector
    participant Pvd as IHardwareLookupProvider Chain
    participant Ext as External APIs (UPC / HPE / Dell / Cisco / Web)

    User->>UI: Enter serial number or UPC, click Search
    UI->>API: POST /api/scraper/search { Query }

    API->>API: AnalyzeQuery — returns Upc if all digits, else SerialNumber
    API->>Svc: LookupDeviceAsync(query, codeType)

    alt codeType == SerialNumber
        Svc->>Det: DetectVendor(query)
        Det-->>Svc: detectedVendor (HPE / Dell / Cisco / empty string)
    else codeType == Upc
        Svc->>Svc: detectedVendor = empty string
    end

    loop For each provider where CanHandle(codeType, vendor) == true
        Svc->>Pvd: SearchAsync(query, detectedVendor)
        Pvd->>Ext: HTTP call to vendor or UPC API
        Ext-->>Pvd: Raw API response
        Pvd-->>Svc: ScrapeResult

        alt ScrapeResult.Success == true
            Svc-->>API: Return ScrapeResult (success path)
        else LookupStatus == manual_lookup_required
            Svc-->>API: Return ScrapeResult with manual lookup URL
        else Provider failed
            Svc->>Svc: Store as lastFailure and continue to next provider
        end
    end

    alt No provider succeeded
        Svc-->>API: ScrapeResult with Success=false and lastFailure details
    end

    API-->>UI: 200 OK with ScrapeResult JSON
    UI-->>User: Display product info or manual lookup hint
```

## Path B — Image-Based Scrape Preview

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant UI as Blazor Scraper Page
    participant API as ScraperController
    participant Scan as ScanService
    participant Svc as ScraperService
    participant Pvd as IHardwareLookupProvider Chain
    participant Ext as External APIs (UPC / HPE / Dell / Cisco / Web)

    User->>UI: Upload device photo
    UI->>API: POST /api/scraper/from-image (multipart form-data)

    API->>Scan: ExtractSerialAsync(imageStream)
    Scan-->>API: extractedCode string

    API->>API: AnalyzeExtractedCode(extractedCode)
    Note right of API: URL detected — not supported for lookup.<br/>All digits — treated as Upc code.<br/>Alphanumeric — treated as SerialNumber.<br/>Anything else — Unknown, cannot lookup.

    alt CanAttemptLookup == false
        API-->>UI: 200 OK with CanAttemptLookup false
        UI-->>User: Show extracted code and reason lookup was skipped
    end

    API->>Svc: LookupDeviceAsync(extractedCode, codeType)
    Svc->>Pvd: Same provider chain as Path A
    Pvd->>Ext: HTTP call to vendor or UPC API
    Ext-->>Pvd: Raw API response
    Pvd-->>Svc: ScrapeResult
    Svc-->>API: ScrapeResult

    API->>API: Map ScrapeResult to ImageScrapePreviewResponse
    API-->>UI: 200 OK with ImageScrapePreviewResponse JSON
    UI-->>User: Show preview with ProductName, Manufacturer, ModelNumber, Image, Source URL
```

## Provider Priority Order

| Priority | Provider | Handles | Notes |
|----------|----------|---------|-------|
| 1 | `UpcLookupProvider` | `Upc` codes | Calls UPC database REST API |
| 2 | `HpeSerialLookupProvider` | `SerialNumber` + vendor=HPE | HPE product lookup API |
| 3 | `DellSerialLookupProvider` | `SerialNumber` + vendor=Dell | Dell support API |
| 4 | `CiscoSerialLookupProvider` | `SerialNumber` + vendor=Cisco | Cisco coverage check API |
| 5 | `WebSearchFallbackProvider` | Any `SerialNumber` | Generic web-search fallback |

`SerialVendorDetector` inspects the serial number prefix/pattern to identify the vendor before the provider chain is consulted, allowing the right vendor-specific provider to be selected first.
