var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var valkey = builder.AddValkey("valkey");

var apiService = builder.AddProject<Projects.AspireApp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

var webApiService = builder.AddProject<Projects.WebApiService>("webapiservice")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WithReference(valkey)
    .WaitFor(cache)
    .WaitFor(valkey)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.AspireApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WithReference(valkey)
    .WaitFor(cache)
    .WaitFor(valkey)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
