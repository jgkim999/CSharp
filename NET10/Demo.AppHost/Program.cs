var builder = DistributedApplication.CreateBuilder(args);

// 애플리케이션 프로젝트들 추가 (직접 경로 방식)
var demoAdmin = builder.AddProject("demo-admin", "../Demo.Admin/Demo.Admin.csproj")
    .WithEnvironment("Postgres__ConnectionString", "Host=192.168.0.47;Port=5432;Database=mydatabase;Username=myuser;Password=strong_password;Maximum Pool Size=8;Minimum Pool Size=2")
    .WithEnvironment("RedisConfig__ConnectionString", "192.168.0.47:6379")
    .WithEnvironment("RabbitMQ__ConnectionString", "amqp://guest:guest@192.168.0.47:5672/")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://192.168.0.47:4317");

var demoWeb = builder.AddProject("demo-web", "../Demo.Web/Demo.Web.csproj")
    .WithEnvironment("Postgres__ConnectionString", "Host=192.168.0.47;Port=5432;Database=mydatabase;Username=myuser;Password=strong_password;Maximum Pool Size=8;Minimum Pool Size=2")
    .WithEnvironment("RedisConfig__ConnectionString", "192.168.0.47:6379")
    .WithEnvironment("RabbitMQ__ConnectionString", "amqp://guest:guest@192.168.0.47:5672/")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://192.168.0.47:4317");

var gamePulse = builder.AddProject("gamepulse", "../GamePulse/GamePulse.csproj")
    .WithEnvironment("Postgres__ConnectionString", "Host=192.168.0.47;Port=5432;Database=mydatabase;Username=myuser;Password=strong_password;Maximum Pool Size=8;Minimum Pool Size=2")
    .WithEnvironment("RedisConfig__ConnectionString", "192.168.0.47:6379")
    .WithEnvironment("RabbitMQ__ConnectionString", "amqp://guest:guest@192.168.0.47:5672/")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://192.168.0.47:4317");

builder.Build().Run();
