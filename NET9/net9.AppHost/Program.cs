var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Demo_WebApi>("demo-webapi").WithReplicas(1);
builder.AddProject<Projects.MudBlazorDemo>("mud-blazor-demo");
builder.AddProject<Projects.OpenApiScalar>("open-api-scalar");

builder.Build().Run();
