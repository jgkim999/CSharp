using Demo.Application;
using Demo.Application.Repositories;
using Demo.Infra;
using Demo.Web.Endpoints.User;
using FastEndpoints;
using FluentValidation;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 환경별 설정 파일 추가
var environment = builder.Environment.EnvironmentName;
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // 환경 변수가 JSON 설정을 오버라이드

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    builder.Services.AddFastEndpoints();
    builder.Services.AddOpenApi();

    builder.Services.AddValidatorsFromAssemblyContaining<UserCreateRequestRequestValidator>();
    
    builder.Services.AddApplication();
    builder.Services.AddInfra(builder.Configuration);

    var app = builder.Build();
    app.UseFastEndpoints(x =>
    {
        x.Errors.UseProblemDetails();
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
