# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

가능하면 응답과 주석은 한국어로 작성한다.

## Solution Structure

This is a .NET 9 solution with Clean Architecture containing two main applications:

### Projects Overview

- **Demo.Web**: Main web application with OpenTelemetry integration and rate limiting
- **GamePulse**: Secondary web application focused on telemetry and RTT metrics
- **Demo.Application**: Application layer with business logic, commands, and services
- **Demo.Infra**: Infrastructure layer with repositories and external services
- **Demo.Application.Tests/GamePulse.Test**: Unit test projects
- **Demo.Web.IntegrationTests**: Integration tests
- **Demo.Web.PerformanceTests**: BenchmarkDotNet performance tests

### Key Architectural Patterns

- **Clean Architecture**: Domain logic separated from infrastructure concerns
- **CQRS with LiteBus**: Commands, queries, and events handled through LiteBus mediator
- **FastEndpoints**: Used instead of traditional MVC controllers
- **OpenTelemetry**: Comprehensive observability with metrics, traces, and logs
- **Repository Pattern**: Data access abstracted through interfaces

## Common Development Commands

### Building

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build Demo.Web/Demo.Web.csproj
dotnet build GamePulse/GamePulse.csproj

# Build in Release mode
dotnet build -c Release
```

### Running Applications

```bash
# Run Demo.Web
cd Demo.Web && dotnet run

# Run GamePulse
cd GamePulse && dotnet run

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Demo.Application.Tests/
dotnet test GamePulse.Test/
dotnet test Demo.Web.IntegrationTests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Performance Testing

```bash
# Run performance benchmarks
cd Demo.Web.PerformanceTests
./Scripts/run-benchmarks.sh

# Quick benchmark mode
./Scripts/run-benchmarks.sh --quick

# Detailed benchmark mode  
./Scripts/run-benchmarks.sh --detailed
```

### Docker Operations

```bash
# Build GamePulse Docker image
cd GamePulse && ./scripts/build-docker.sh

# Build with specific tag
./scripts/build-docker.sh --tag v1.0.0

# Build and push to registry
./scripts/build-docker.sh --tag v1.0.0 --push
```

## Key Technologies and Frameworks

### Core Framework Stack

- **.NET 9**: Target framework
- **FastEndpoints**: API endpoint framework (replaces traditional controllers)
- **FluentValidation**: Input validation
- **Mapster**: Object mapping
- **Serilog**: Structured logging

### Messaging and CQRS

- **LiteBus**: Mediator for commands, queries, and events
- Custom telemetry decorators for LiteBus operations

### Observability (OpenTelemetry)
- **Metrics**: Custom metrics collection and Prometheus exports
- **Tracing**: Distributed tracing with OTLP export
- **Logging**: Serilog integration with OpenTelemetry

### Data and Caching

- **PostgreSQL**: Primary database (via Npgsql)
- **Redis**: Caching and session storage
- **StackExchange.Redis**: Redis client

### Authentication and Security

- **FastEndpoints.Security**: JWT-based authentication
- **Rate Limiting**: Custom middleware implementation

## Project-Specific Notes

### Demo.Web

- Contains OpenTelemetry configuration extensions
- Implements custom rate limiting middleware
- Uses environment-specific configuration files
- Includes comprehensive performance benchmarking

### GamePulse  
- Focuses on RTT (Round Trip Time) metrics collection
- Contains IP geolocation services using IP2Location
- Implements background task queuing for SOD operations
- Has Docker containerization setup

### Configuration Management

- Uses `appsettings.json` and environment-specific overrides
- Configuration classes in `Demo.Application/Configs/`
- Key configs: `JwtConfig`, `RedisConfig`, `OtelConfig`, `RateLimitConfig`

### Testing Strategy

- Unit tests for application services and repositories
- Integration tests for web endpoints
- Performance benchmarks with specific criteria:
  - Application startup time: <10% increase with OpenTelemetry
  - HTTP request processing: <5% increase with OpenTelemetry  
  - Memory usage: <50MB increase
  - Minimum throughput: 100 req/s

## Development Guidelines

### Service Registration

- Application services registered in `ApplicationInitialize.cs`
- Infrastructure services registered in `InfraInitialize.cs`
- Follow existing DI container patterns

### Adding New Endpoints

- Use FastEndpoints pattern (see existing endpoints in `Endpoints/` folders)
- Include request/response DTOs with FluentValidation
- Follow versioning convention with `V1`, `V2` suffixes

### Observability Integration

- Use existing telemetry decorators for LiteBus operations
- Add custom metrics through `ITelemetryService`
- Follow OpenTelemetry naming conventions for spans and metrics

### Background Processing

- Use `ISodBackgroundTaskQueue` for async operations
- Background workers already configured in `SodBackgroundWorker`
