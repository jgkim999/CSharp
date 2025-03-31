var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<RedisResource> redis = builder.AddRedis("demo-cache");

builder.AddProject<Projects.Demo_WebApi>("demo-webapi")
    .WithReplicas(1)
    .WithReference(redis);

builder.AddProject<Projects.MudBlazorDemo>("mud-blazor-demo")
    .WithReference(redis);

builder.AddProject<Projects.OpenApiScalar>("open-api-scalar")
    .WithReference(redis);

builder.Build().Run();
