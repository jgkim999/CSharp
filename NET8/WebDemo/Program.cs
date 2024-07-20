using Quartz;

using Serilog;
using Serilog.Core;

internal class Program
{
    public static void Main(string[] args)
    {
        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(
                    $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

            var logLevelSwitch = new LoggingLevelSwitch();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(logLevelSwitch)
                .ReadFrom.Configuration(configuration)
                .WriteTo.Seq(
                    "http://192.168.0.47:10000/",
                    bufferBaseFilename: @"./Logs/log_buffer",
                    controlLevelSwitch: logLevelSwitch)
                .CreateLogger();

            TimeZoneInfo localZone = TimeZoneInfo.Local;
            Log.Information("Local Time Zone ID:{0}", localZone.Id);
            Log.Information("Display Name is:{0}.", localZone.DisplayName);
            Log.Information("Standard name is:{0}.", localZone.StandardName);
            Log.Information("Daylight saving name is:{0}.", localZone.DaylightName);
            
            Log.Information("UserName:{0}", Environment.UserName);
            Log.Information("MachineName:{0}", Environment.MachineName);
            Log.Information("OSVersion:{0}", Environment.OSVersion);
            Log.Information("Version:{0}", Environment.Version);
            Log.Information("CurrentDirectory:{0}", Environment.CurrentDirectory);
            Log.Information("SystemDirectory:{0}", Environment.SystemDirectory);
            Log.Information("UserDomainName:{0}", Environment.UserDomainName);
            Log.Information("UserInteractive:{0}", Environment.UserInteractive);
            Log.Information("ProcessorCount:{0}", Environment.ProcessorCount);
            Log.Information("ASPNETCORE_ENVIRONMENT:{0}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog(Log.Logger);

            // Add services to the container.
            builder.Services.AddQuartz();
            builder.Services.AddQuartzHostedService(opt => { opt.WaitForJobsToComplete = true; });


            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSerilogRequestLogging();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
