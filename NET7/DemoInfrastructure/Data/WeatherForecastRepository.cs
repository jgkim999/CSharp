using DemoApplication.Interfaces;
using DemoDomain.Entities;
using Bogus;
using Microsoft.Extensions.Logging;

namespace DemoInfrastructure.Data;

public class WeatherForecastRepository : IWeatherForecastRepository
{
    private readonly ILogger<WeatherForecastRepository> _logger;
    private readonly Faker _faker = new();

    public WeatherForecastRepository(ILogger<WeatherForecastRepository> logger)
    {
        _logger = logger;
    }
    
    public async Task<IEnumerable<WeatherForecast>> SelectAsync()
    {
        await Task.CompletedTask;
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = _faker.Lorem.Sentences(5)
            })
            .ToArray();
    }
}