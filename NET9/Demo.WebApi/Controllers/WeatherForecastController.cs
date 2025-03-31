using Microsoft.AspNetCore.Mvc;

namespace Demo.WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }

    [HttpGet(Name = "GetWeatherForecast2")]
    public ActionResult<WeatherForecast> Get2()
    {
        return Problem(
            type: "Bad Request",
            title: "Identity failure",
            detail: "wtf",
            statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpGet(Name = "GetWeatherForecast3")]
    public ActionResult<WeatherForecast> Get3()
    {
        throw new ArgumentException("This is a test exception");
    }
}