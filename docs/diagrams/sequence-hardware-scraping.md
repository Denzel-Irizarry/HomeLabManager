# Sequence Diagram — Hardware Scraping / Lookup Pipeline

This diagram shows how the scraper feature works — either from a text query
(serial number or UPC) entered on the Scraper page, or from an uploaded image.

## Path A — Text Search

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant WEBUI as Blazor UI<br/>(Scraper page)
    participant API as ScraperController<br/>POST /api/scraper/search
    participant ScraperSvc as ScraperService
    participant Detector as SerialVendorDetector
    participant Providers as IHardwareLookupProvider chain
    participant ExtAPI as External APIs<br/>(UPC DB / HPE / Dell / Cisco / Web)

    User->>WEBUI: Enter serial number or UPC, click "Search"
    WEBUI->>API: POST /api/scraper/search {Query: "..."}

    API->>API: AnalyzeSearchQuery(query)<br/>→ "Upc" if all digits, else "SerialNumber"
    API->>ScraperSvc: LookupDeviceAsync(query, codeType)

    alt codeType == "SerialNumber"
        ScraperSvc->>Detector: DetectVendor(query)
        Detector-->>ScraperSvc: detectedVendor (e.g. "HPE", "Dell", "Cisco")
    else codeType == "Upc"
        ScraperSvc->>ScraperSvc: detectedVendor = ""
    end

    loop For each provider where CanHandle(codeType, detectedVendor) == true
        ScraperSvc->>Providers: SearchAsync(query, detectedVendor)
        Providers->>ExtAPI: HTTP call to vendor / UPC API
        ExtAPI-->>Providers: raw response
        Providers-->>ScraperSvc: ScrapeResult

        alt result.Success == true
            ScraperSvc-->>API: return ScrapeResult (success)
        else result.LookupStatus == "manual_lookup_required" with URL
            ScraperSvc-->>API: return ScrapeResult (manual lookup hint)
        else result failed
            ScraperSvc->>ScraperSvc: Store as lastFailure, continue to next provider
        end
    end

    alt No provider succeeded
        ScraperSvc-->>API: ScrapeResult{Success=false, lastFailure details}
    end

    API-->>WEBUI: 200 OK (ScrapeResult JSON)
    WEBUI-->>User: Display product info or manual lookup hint
```

## Path B — Image-Based Scrape Preview

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant WEBUI as Blazor UI<br/>(RegisterImage.razor preview step)
    participant API as ScraperController<br/>POST /api/scraper/from-image
    participant ScanSvc as ScanService
    participant ScraperSvc as ScraperService
    participant Providers as IHardwareLookupProvider chain
    participant ExtAPI as External APIs

    User->>WEBUI: Upload device image
    WEBUI->>API: POST /api/scraper/from-image (multipart/form-data)

    API->>ScanSvc: ExtractSerialAsync(ScanRequest{imageStream})
    ScanSvc-->>API: extractedCode (string)

    API->>API: AnalyzeExtractedCode(extractedCode)
    note right of API: Returns (CodeType, CanAttemptLookup, Message)<br/>URL → not supported<br/>All digits → "Upc", can lookup<br/>Alphanumeric → "SerialNumber", can lookup<br/>Other → "Unknown", cannot lookup

    alt CanAttemptLookup == false
        API-->>WEBUI: 200 OK ImageScrapePreviewResponse{CanAttemptLookup:false}
        WEBUI-->>User: Show extracted code + reason no lookup attempted
    end

    API->>ScraperSvc: LookupDeviceAsync(extractedCode, codeType)
    ScraperSvc->>Providers: (same provider chain as Path A)
    Providers->>ExtAPI: HTTP call
    ExtAPI-->>Providers: raw response
    Providers-->>ScraperSvc: ScrapeResult
    ScraperSvc-->>API: ScrapeResult

    API->>API: Map ScrapeResult → ImageScrapePreviewResponse
    API-->>WEBUI: 200 OK ImageScrapePreviewResponse
    WEBUI-->>User: Show preview (ProductName, Manufacturer,<br/>ModelNumber, Image, Source URL)
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
