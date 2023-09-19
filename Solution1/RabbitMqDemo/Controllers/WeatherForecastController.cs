using Microsoft.AspNetCore.Mvc;
using MassTransit;
using RabbitMqDemo.Models;

namespace RabbitMqDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private ILogger<WeatherForecastController> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        await _publishEndpoint.Publish<Weather>(new Weather(){ City = "Seoul", Celsius = Random.Shared.Next(-20, 55) });
        
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}