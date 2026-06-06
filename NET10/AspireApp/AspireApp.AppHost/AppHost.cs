var builder = DistributedApplication.CreateBuilder(args);

//var cache = builder.AddRedis("cache")
//    .WithEndpoint(port: 6378, targetPort: 6379, name: "tcp"); ;

var cacheRedis = builder.AddRedis("redis")
    .WithDataVolume(isReadOnly: false)
    .WithRedisInsight();
    
var mysqlPassword = builder.AddParameter("password", secret: true);

var mysql = builder.AddMySql("mysql", mysqlPassword)
    .WithDataVolume("mysql-data")
    .WithEndpoint(port: 3306, targetPort: 3306, name: "tcp")
    .WithInitFiles(Path.Combine(AppContext.BaseDirectory, "data", "init.sql"))
    .WithPhpMyAdmin();

var mydb = mysql.AddDatabase("mydb");
    
var apiService = builder.AddProject<Projects.AspireApp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    //.WithReference(cache)
    .WithReference(cacheRedis)
    //.WaitFor(cache)
    .WaitFor(cacheRedis);

builder.AddProject<Projects.AspireApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    //.WithReference(cache)
    .WithReference(cacheRedis)
    //.WaitFor(cache)
    .WaitFor(cacheRedis)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.WebApiService>("WebApiService")
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar (HTTP)";
        url.Url = "/scalar";
    })
    .WithHttpHealthCheck("/health")
    .WithReference(cacheRedis)
    .WaitFor(cacheRedis)
    .WithReference(mysql)
    .WaitFor(mysql)
    .WithReference(mydb)
    .WaitFor(mydb);

builder.Build().Run();
