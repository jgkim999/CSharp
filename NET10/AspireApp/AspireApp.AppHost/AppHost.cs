var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var valkey = builder.AddValkey("valkey");

var mysqlPassword = builder.AddParameter("password", secret: true);

var mysql = builder.AddMySql("mysql", mysqlPassword)
    //.WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(port: 3306, targetPort: 3306, name: "tcp")
    .WithInitFiles(Path.Combine(AppContext.BaseDirectory, "data", "init.sql"))
    .WithPhpMyAdmin();
    
var apiService = builder.AddProject<Projects.AspireApp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WithReference(valkey)
    .WaitFor(cache)
    .WaitFor(valkey);

builder.AddProject<Projects.AspireApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WithReference(valkey)
    .WaitFor(cache)
    .WaitFor(valkey)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.WebApiService>("WebApiService")
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar (HTTP)";
        url.Url = "/scalar";
    })
    .WithHttpHealthCheck("/health")
    .WithReference(valkey)
    .WaitFor(valkey)
    .WithReference(mysql)
    .WaitFor(mysql);

builder.Build().Run();
