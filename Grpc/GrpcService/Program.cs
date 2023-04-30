using GrpcService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
// Serilog
// https://github.com/datalust/dotnet6-serilog-example?ref=blog.datalust.co
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();
// Serilog
// https://github.com/datalust/dotnet6-serilog-example?ref=blog.datalust.co
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
