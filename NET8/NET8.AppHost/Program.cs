var builder = DistributedApplication.CreateBuilder(args);

var apiservice = builder.AddProject<Projects.NET8_ApiService>("apiservice");

builder.AddProject<Projects.NET8_Web>("webfrontend")
    .WithReference(apiservice);

builder.Build().Run();
