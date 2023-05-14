using CacheSample.Application.Abstractions.Caching;
using Microsoft.AspNetCore.Mvc;

namespace CacheSample.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ICacheService _cacheService;

    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, ICacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        List<WeatherForecast>? weatherForecast = await _cacheService.GetAsync<List<WeatherForecast>>(
            "WeatherForecast",
            async () =>
            {
                var weathers = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                    })
                    .ToList();
                return weathers;
            });
        return weatherForecast;
    }
}