# HomeLabManager

HomeLabManager is my personal home lab inventory app.

## Why This Project Matters to Me

This project is where I practice building software the way I actually want to ship it: layered, testable, and useful in real life. It is not just tutorial code. I use it to keep track of my devices without relying on random spreadsheets. It also doubles as a real full-stack .NET project where I can keep improving architecture, API design, testing, and UI decisions as I go.

## What It Does Right Now

- Register devices manually
- Register devices from image upload (barcode/QR workflow)
- Store and edit device details (serial, nickname, location)
- Track product + vendor details tied to devices
- Manage components and link components to devices
- View dashboard stats
- View vendor data and deduplication results
- Manage UI preferences from Settings (theme + density)
- Test API endpoints in Swagger

## Current Tech Stack

- .NET 10
- ASP.NET Core Web API
- Blazor Server (Interactive Server)
- Entity Framework Core + SQLite
- Swagger / OpenAPI
- ZXing.Net + ImageSharp
- xUnit tests for service and scraping flows

## Solution Layout

```text
HomeLabManager/
|-- HomeLabManager.Core/
|   |-- Entities/
|   |-- Scraping/
|
|-- HomeLabManager.API/
|   |-- Controllers/
|   |-- Services/
|   |   |-- Scraping/
|   |       |-- Providers/
|   |-- Infrastructure/
|   |-- Interfaces/
|   |-- Models/
|   |-- Migrations/
|
|-- HomeLabManager.WEBUI/
|   |-- Components/
|   |   |-- Layout/
|   |   |-- Pages/
|   |-- Models/
|   |-- wwwroot/
|
|-- HomeLabManager.API.Tests/
|   |-- Services/
|
|-- docs/
|   |-- project-structure-uml.md
|
|-- HomeLabManager.slnx
```

## Architecture (How Requests Flow)

1. Blazor Web UI sends request to API.
2. API Controller receives request.
3. Service layer handles business logic.
4. Repository layer + DbContext handle persistence.
5. SQLite stores final data.

I kept this layered on purpose so each part has one job and I can test/refactor without ripping everything apart.

## Scraping/Lookup Pipeline

For image-based registration and serial lookups, the API uses a provider pipeline behind interfaces.

- Vendor/provider routing goes through scraper services
- Includes vendor-specific providers (HPE, Dell, Cisco, UPC, fallback)
- Can be tested with fake providers in unit tests

This lets me expand vendor support without rewriting core registration logic.

## Main Pages in Web UI

- Dashboard
- Devices
- Manual Register
- Register by Image
- Components
- Vendors
- Settings

## Run It Locally

### 1) Prerequisites

- .NET SDK 10

### 2) Restore

```bash
dotnet restore
```

### 3) Run Everything With One Command (Recommended)

If you want both API + WEBUI started together from the repo root:

PowerShell (Windows):

```powershell
./run-local.ps1
```

Bash (Git Bash/WSL/macOS/Linux):

```bash
./run-local.sh
```

This starts both projects and stops both when you exit.

### 4) Run API (Manual Option)

```bash
dotnet run --project HomeLabManager.API/HomeLabManager.API.csproj
```

Swagger will be available from the API host URL shown in the terminal.

### 5) Run Web UI (Manual Option)

```bash
dotnet run --project HomeLabManager.WEBUI/HomeLabManager.WEBUI.csproj
```

The UI talks to the API through configured HttpClient settings in app configuration.

## Run Tests

```bash
dotnet test HomeLabManager.API.Tests/HomeLabManager.API.Tests.csproj
```

## Notes

- This is an actively evolving project, so I update architecture and UX as I learn.
- The UML doc in docs captures current structure at a high level.


