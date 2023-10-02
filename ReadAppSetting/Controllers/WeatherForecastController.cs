using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ReadAppSetting.Settings;

namespace ReadAppSetting.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly FormatSettings _formatSettings;
    private readonly DatabaseSettings _databaseSettings;

    public WeatherForecastController(
        ILogger<WeatherForecastController> logger,
        IOptions<FormatSettings> options,
        IOptions<DatabaseSettings> databases)
    {
        _logger = logger;
        _formatSettings = options.Value;
        _databaseSettings = databases.Value;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        _logger.LogInformation($"Localize:{_formatSettings.Localize}");
        _logger.LogInformation($"Number.Format:{_formatSettings.Number?.Format}");
        _logger.LogInformation($"Number.Precision:{_formatSettings.Number?.Precision}");
        _logger.LogInformation($"Database.Write:{_databaseSettings.Write}");
        _logger.LogInformation($"Database.Read:{_databaseSettings.Read}");

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}