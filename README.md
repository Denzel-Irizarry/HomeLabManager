# HomeLabManager

HomeLabManager is a personal project I built to help manage hardware in a home lab. The goal of the project is to keep track of devices, their vendor and product information, and the individual components installed in them. I wanted to build something practical that also helped me learn more about full-stack .NET development, API design, database access, and frontend development with Blazor.

## What This Project Does

This application allows me to:

- Register devices manually
- Register devices by uploading an image with a barcode or QR code
- Store device details such as serial number, nickname, and location
- Track product and vendor information for each device
- Create and manage a list of hardware components
- Link components to devices
- View device statistics from a dashboard
- Use Swagger to test API endpoints

## Why I Built It

I created this project as a way to combine a real-world use case with the technologies I have been learning. Instead of building a generic CRUD app, I wanted to make something that felt useful to me personally. Since I have an interest in home lab environments and IT hardware, I decided to build a system that could help organize devices and components in one place.

This project also gave me a chance to practice:

- Designing a multi-project .NET solution
- Building REST API endpoints with ASP.NET Core
- Using Entity Framework Core with SQLite
- Creating a Blazor Server frontend
- Working with dependency injection
- Handling file uploads and image processing
- Organizing code into layers such as entities, services, repositories, and controllers

## Project Structure

```text
HomeLabManager/
|-- HomeLabManager.Core/
|   |-- Entities/
|
|-- HomeLabManager.API/
|   |-- Controllers/
|   |-- Services/
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
|-- HomeLabManager.slnx
```
### HomeLabManager.Core
This project contains the shared entity classes used across the application. These classes represent the main data in the system.
I used this project to keep the domain models separate from the API and frontend so the solution is easier to organize and maintain.

### HomeLabManager.API
This is the backend of the application. It is an ASP.NET Core Web API that handles requests from the frontend, applies business logic, and saves data to the database.

This project includes:
- Controllers for API endpoints
- Services for business logic
- Repositories for database access
- EF Core database context
- Request and response models
- Database migrations
The API also includes the image scanning workflow for uploaded files. It uses barcode/QR processing to extract a serial number and then continues the device registration process.

### HomeLabManager.WEBUI
This is the frontend of the application built with Blazor Server. It provides the user interface for interacting with the system.

The frontend includes pages for:

- Dashboard
- Viewing all devices
- Manual device registration
- Image-based registration
- Components
- Vendors
- Settings
The Web UI communicates with the API using HttpClient.

## How The App Works
The app follows a layered design:

1. The user interacts with the Blazor frontend.
2. The frontend sends requests to the API.
3. Controllers receive the request and call service classes.
4. Services handle the business rules and workflows.
5. Repositories and Entity Framework Core interact with the SQLite database.
This structure helped me separate responsibilities and keep the code more organized.

### Technologies Used
- .NET 10
- ASP.NET Core Web API
- Blazor Server
- Entity Framework Core(used to interact with the SQLite)
- SQLite
- Swagger / OpenAPI(used for testing and verifying endpoint connnections)
- ImageSharp
- ZXing.Net

### What I Learned
Some of the main things I learned while working on this project were:

- How to separate a solution into frontend, backend, and shared model projects
- How to build API endpoints and connect them to a UI
- How to use Entity Framework Core for relationships and migrations
- How dependency injection helps connect services and repositories
- How to handle validation and exception handling in an API
- How to upload and process files in a web application
- How to design a project around a real use case instead of a simple tutorial example
