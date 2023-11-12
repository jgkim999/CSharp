using Bogus;
using DemoApplication.Handlers;
using DemoApplication.Mq;
using DemoDomain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DemoWebApi.Controllers;

[ApiController]
[Route("v1/api/[controller]/[action]")]
public class MqPublishController : ControllerBase
{
    private readonly ILogger<MqPublishController> _logger;
    private readonly MqProducer _produce;
    private readonly Faker _faker;
    
    public MqPublishController(ILogger<MqPublishController> logger, MqProducer producer)
    {
        _logger = logger;
        _produce = producer;
        _faker = new Faker(locale: "ko");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> Publish()
    {
        var weather = new WeatherForecast
        {
            City = _faker.Address.City(),
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = _faker.Lorem.Sentences(1)
        };
        string msg = JsonConvert.SerializeObject(weather);
        _logger.LogInformation($"Publish {msg}");
        _produce.Publish(exchange: "test", routingKey: "#",msg: msg);
        await Task.CompletedTask;
        return Ok();
    }
}
