using DemoApplication;
using DemoDomain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DemoWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IMediator _mediator;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> Get()
    {
        var result = await _mediator.Send(new WeatherForcastSelect());
        return Ok(result);
    }
}