using System.Reflection;
using DemoApplication.Interfaces;
using DemoInfrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();