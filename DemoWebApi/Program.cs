using System.Reflection;
using DemoApplication.Interfaces;
using DemoApplication.Middlewares;
using DemoApplication.Settings;
using DemoInfrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormatSettings>(builder.Configuration.GetSection("Formatting"));
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.Services.AddMediatR(cfg => {
//     cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
// });

builder.Services.AddTransient<IWeatherForecastRepository, WeatherForecastRepository>();
//builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetAssembly(Program)));
builder.Services.AddMediatR(cfg =>
{
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
    foreach (var x in assemblies)
    {
        cfg.RegisterServicesFromAssembly(x);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global error handler
app.UseMiddleware<ErrorHandlingMiddleware>();

//app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();