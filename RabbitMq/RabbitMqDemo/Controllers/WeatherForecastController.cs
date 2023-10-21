using Microsoft.AspNetCore.Mvc;
using MassTransit;
using MassTransit.DependencyInjection;
using RabbitMqDemo.Models;

namespace RabbitMqDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private ILogger<WeatherForecastController> _logger;
    private readonly IPublishEndpoint _ep1;
    readonly Bind<ISecondBus, IPublishEndpoint> _ep2;
    
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public WeatherForecastController(
        ILogger<WeatherForecastController> logger,
        IPublishEndpoint ep1,
        Bind<ISecondBus, IPublishEndpoint> ep2)
    {
        _logger = logger;
        _ep1 = ep1;
        _ep2 = ep2;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        await _ep1.Publish<Weather>(
            new Weather(){ City = "Seoul", Celsius = Random.Shared.Next(-20, 55) });
        await _ep2.Value.Publish<Weather2>(
            new Weather2(){ City = "Daegu", Celsius = Random.Shared.Next(-20, 55) });
        
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}
