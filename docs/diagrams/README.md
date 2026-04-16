# HomeLabManager — UML Diagrams

This folder contains Mermaid-based UML diagrams that visualize the structure,
architecture, and key workflows of the HomeLabManager project.
All diagrams render directly on GitHub and in any Mermaid-compatible viewer.

## Diagram Index

| File | Type | What it shows |
|------|------|---------------|
| [class-diagram-domain-entities.md](class-diagram-domain-entities.md) | Class diagram | Core domain model — all five entities and their relationships |
| [class-diagram-api-layer.md](class-diagram-api-layer.md) | Class diagram | Full API layer — controllers, services, repositories, interfaces, and scraping providers |
| [component-diagram-system-overview.md](component-diagram-system-overview.md) | Component diagram | High-level system overview — projects, runtime components, and data flow |
| [sequence-image-device-registration.md](sequence-image-device-registration.md) | Sequence diagram | Image-based device registration (upload photo → extract serial → vendor lookup → save) |
| [sequence-manual-device-registration.md](sequence-manual-device-registration.md) | Sequence diagram | Manual device registration (form submit → validate → save) |
| [sequence-hardware-scraping.md](sequence-hardware-scraping.md) | Sequence diagram | Hardware lookup / scraping provider pipeline (search or image → provider chain → result) |

## Quick Architecture Summary

```
HomeLabManager.WEBUI  (Blazor Server)
        │  HTTP REST
        ▼
HomeLabManager.API    (ASP.NET Core)
   ├── Controllers
   ├── Services  ──────────── HomeLabManager.Core  (shared entities & DTOs)
   ├── Repositories
   └── Scraping Providers
        │  EF Core
        ▼
     SQLite DB
```

See the individual files for full diagram details.
