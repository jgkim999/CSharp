var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Demo_WebApi>("demo-webapi");

builder.Build().Run();
