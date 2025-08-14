# GEMINI Project Context

## Project Overview

This repository contains a .NET 9 solution named `net9` with a focus on web services and modern .NET development practices. The primary project is `Demo.WebApi`, a web application built with ASP.NET Core.

The solution is structured into the following layers:

* **`Demo.Application`**: Contains the application logic, including commands, queries, and services.
* **`Demo.Infra`**: Manages infrastructure concerns, such as database access and other external services.
* **`Demo.WebApi`**: The main web application project that exposes the API endpoints.
* **`net9.ServiceDefaults`**: A project for service defaults.

## Building and Running

To build and run the projects, you will need the .NET 9 SDK.

### Building the Solution

```bash
dotnet build net9.sln
```

### Running the Web Application

You can run the `Demo.WebApi` project.

```bash
dotnet run --project Demo.WebApi/Demo.WebApi.csproj
```

## Development Conventions

* **Centralized Package Management**: The project uses a `_Directory.Packages.props` file to manage NuGet package versions across the solution.
* **Structured Logging**: The `Demo.WebApi` project is configured with Serilog for structured logging.
* **Configuration**: Application settings are managed through `appsettings.json` files for different environments (Development, Production).
* **Dependency Injection**: The projects make extensive use of dependency injection, with services registered in `Program.cs` and other initialization files.
