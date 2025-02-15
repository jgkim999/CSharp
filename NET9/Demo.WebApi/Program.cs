using Demo.Application;

using Scalar.AspNetCore;

using Serilog;

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
            builder.AddServiceDefaults();

            builder.AddApplicationServices("DemoWebApi");

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            
            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

            }

            app.UseAuthorization();
            // {Scheme}://{ServiceHost}:{ServicePort}/scalar/v1
            app.MapScalarApiReference(options =>
            {
                options.Servers =
                [
                    new ScalarServer(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
                ];
            });
            app.MapControllers();
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
