var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var valkey = builder.AddValkey("valkey");

var mysql = builder.AddMySql("mysql").WithLifetime(ContainerLifetime.Persistent);
var mysqldb = mysql.AddDatabase("mydb");

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
    .WithReference(mysqldb)
    .WaitFor(mysqldb); ;

builder.Build().Run();
