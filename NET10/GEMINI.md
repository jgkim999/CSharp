# GEMINI Project Context

## Project Overview

This repository contains a .NET 9 solution named `DotNetDemo` with a focus on web services, observability, and performance. The primary projects are `GamePulse` and `Demo.Web`, both of which are web applications built using the FastEndpoints framework.

The solution is structured into the following layers:

* **`Demo.Application`**: Contains the application logic, including commands, queries, and services.
* **`Demo.Infra`**: Manages infrastructure concerns, such as database access (PostgreSQL) and caching (Redis).
* **`Demo.Web`**: A web application project that demonstrates various features, including rate limiting and OpenTelemetry integration.
* **`GamePulse`**: Another web application project, likely the main application, with detailed OpenTelemetry setup and integration with IP2Location services.
* **`terraform`**: Contains Terraform scripts for deploying the `GamePulse` application to AWS ECS.

A key feature of this project is its extensive use of **OpenTelemetry** for distributed tracing, metrics, and logging. The configuration is highly customizable and managed through `appsettings.json` and environment variables.

## Building and Running

To build and run the projects, you will need the .NET 9 SDK.

### Building the Solution

```bash
dotnet build DotNetDemo.slnx
```

### Running the Web Applications

You can run either the `Demo.Web` or `GamePulse` project.

**To run `Demo.Web`:**

```bash
dotnet run --project Demo.Web/Demo.Web.csproj
```

**To run `GamePulse`:**

```bash
dotnet run --project GamePulse/GamePulse.csproj
```

### Running Tests

The solution includes integration and performance tests.

**Integration Tests:**

```bash
dotnet test Demo.Web.IntegrationTests/Demo.Web.IntegrationTests.csproj
```

**Performance Tests:**

The `k6` directory contains a load test script. To run it, you will need to have k6 installed.

```bash
k6 run k6/load-test.js
```

## Development Conventions

* **Web Framework**: The project uses [FastEndpoints](https://fast-endpoints.com/) for building web APIs.
* **Observability**: OpenTelemetry is the standard for logging, tracing, and metrics. The configuration is centralized in `OpenTelemetryInitialize.cs` and `OpenTelemetryConfig.cs`.
* **Infrastructure as Code**: The infrastructure is managed using Terraform. The configuration is located in the `terraform` directory.
* **Configuration**: Application settings are managed through `appsettings.json` files for different environments (Development, Production).
* **Dependency Injection**: The projects make extensive use of dependency injection, with services registered in `Program.cs` and other initialization files.
