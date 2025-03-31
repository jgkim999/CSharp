using Serilog;

using WebDemo.Application;
using WebDemo.Infra;

internal class Program
{
    public static void Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.AddOpenTelemetry(x =>
            {
                x.IncludeScopes = true;
                x.IncludeFormattedMessage = true;
            });
            builder.Services.AddSerilog();

            // Seq
            builder.Services.AddApplicationOpenTelemetry("WebDemoConsumer", "t2lnjaPH8UrzSxRgwcCZ");

            builder.Services.AddHealthChecks();
            {
                builder.WebHost.ConfigureKestrel((context, serverOptions) =>
                {
                    var kestrelSection = context.Configuration.GetSection("Kestrel");
                    serverOptions.Configure(kestrelSection);
                });
                Log.Information("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
                Log.Information("ASPNETCORE_URLS", Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
            }

            // Add services to the container.

            builder.Services.AddApplicationServices("WebDemoConsumer", "1.0.0");
            builder.Services.AddMassTransItConsumer();
            builder.Services.AddInfraServices();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSerilogRequestLogging();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI();
            //}

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.MapHealthChecks("/healthz");

            app.MapPrometheusScrapingEndpoint();

            app.Run();
        }
        catch (Exception e)
        {
            Log.Error(e, "Unhandled Exception");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
