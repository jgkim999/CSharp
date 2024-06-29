using Quartz;

using Serilog;

internal class Program
{
    public static void Main(string[] args)
    {
        TimeZoneInfo localZone = TimeZoneInfo.Local;
        Console.WriteLine("Local Time Zone ID: {0}", localZone.Id);
        Console.WriteLine("   Display Name is: {0}.", localZone.DisplayName);
        Console.WriteLine("   Standard name is: {0}.", localZone.StandardName);
        Console.WriteLine("   Daylight saving name is: {0}.", localZone.DaylightName);

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
        builder.Services.AddQuartz();
        builder.Services.AddQuartzHostedService(opt =>
        {
            opt.WaitForJobsToComplete = true;
        });


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
}
