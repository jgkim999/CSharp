# .NET 9 Development Guidelines

## Build Commands
- Build: `dotnet build`
- Run project: `dotnet run --project [ProjectName]`
- Run specific test class: `dotnet test --filter "FullyQualifiedName~[TestClassName]"`
- Run specific test method: `dotnet test --filter "Name=[TestMethodName]"`
- Check unit tests: `dotnet test Demo.xUnitTest/Demo.xUnitTest.csproj`

## Code Style Guidelines
- **Formatting**: Use file-scoped namespaces; braces on new lines; 4 spaces indentation
- **Types**: Use strong typing with `required` for mandatory properties
- **Naming**: PascalCase for types/methods/properties; camelCase for variables/parameters
- **Error Handling**: Use `FluentResults` pattern for methods returning complex results
- **Imports**: Organize using directives by System > third-party > project namespaces
- **Testing**: Use xUnit with Fact/Theory attributes, MSTest with TestMethod attribute
- **Exception Handling**: Use try/catch with proper logging via Serilog in app classes
- **Models**: Use data annotations for validation requirements and DB relationships
- **Interfaces**: Prefix with "I", keep focused on single responsibility
- **Async**: Use async/await pattern consistently, include cancellation tokens when possible

## Logging
- Use Serilog with structured logging
- Log errors with contextual information (not just exceptions)