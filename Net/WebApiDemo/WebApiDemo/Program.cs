using HealthChecks.ApplicationStatus.DependencyInjection;
using HealthChecks.UI.Client;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using MySqlConnector;
using Quartz;

using Serilog;
using Serilog.Extensions.Logging;
using StackExchange.Redis;
using System.Reflection;
using System.Threading.RateLimiting;
using WebApiApplication.Interfaces;
using WebApiApplication.Services;
using WebApiDemo.Extensions;
using WebApiDemo.HealthChecks;
using WebApiDemo.SchedulingJobs;
using WebApiInfrastructure;
using WebApiInfrastructure.Repositories;

internal class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(e => e.Console())
            .CreateBootstrapLogger();

        try
        {
            var envJson = $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json";

            Log.Information("Starting web application");
            Log.Information($"{envJson}");

            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile(envJson);

            builder.Host.UseSerilog((ctx, config) =>
            {
                config.ReadFrom.Configuration(ctx.Configuration);
            });
            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHealthChecksUI().AddInMemoryStorage();
            builder.Services.AddHealthChecks()
                .AddCheck<RandomHealthCheck>("random")
                .AddApplicationStatus();

            builder.Services.AddRateLimiter(options =>
            {
                // IP별로 요청 제한
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("fixed", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromSeconds(10)
                        }));
            });

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = ".Net Core WebAPI",
                    Description = ".Net Core WebAPI Template",
                    TermsOfService = new Uri("https://termly.io/resources/templates/terms-of-service-template/"),
                    Contact = new OpenApiContact
                    {
                        Name = "Example Contact",
                        Url = new Uri("https://example.com/contact")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Example License",
                        Url = new Uri("https://example.com/license")
                    }
                });

                // using System.Reflection;
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            builder.Services.AddQuartz(q =>
            {
                //q.UseMicrosoftDependencyInjectionJobFactory();

                // 1분마다 영원히 실행
                var jobKey = new JobKey("HelloJob");
                q.AddJob<HelloJob>(opts => opts.WithIdentity(jobKey));

                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("trigger1", "group1")
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever()));
            });
            builder.Services.AddQuartzHostedService(o =>
            {
                o.WaitForJobsToComplete = true;
            });

            builder.Services.AddTransient(x => new MySqlConnection(builder.Configuration.GetConnectionString("Default")));
            builder.Services.AddTransient<IAccountCache, AccountCache>();
            builder.Services.AddTransient<IAccountRepository, AccountRepository>();
            builder.Services.AddTransient<IAccountService, AccountService>();

            var redisManagerLogger = new SerilogLoggerFactory(Log.Logger).CreateLogger<RedisManager>();
            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis"));
            RedisManager redisManager = new RedisManager(redisManagerLogger, connectionMultiplexer);
            builder.Services.AddSingleton<IRedisManager>(redisManager);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseHttpLogging();
                app.UseCustomRequestLoggingMiddleware();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseCustomExceptionMiddleware();

            // http://[domain]/healthz
            app.UseHealthChecks("/healthz", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            // http://[domain]/healthchecks-ui
            app.UseHealthChecksUI();

            app.MapControllers();

            app.UseRateLimiter();

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
    }
}
