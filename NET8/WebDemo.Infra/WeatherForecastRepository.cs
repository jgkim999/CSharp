using System.Diagnostics;
using WebDemo.Application;
using WebDemo.Application.Repositories;
using WebDemo.Domain.Models;

namespace WebDemo.Infra;

public class WeatherForecastRepository : IWeatherForecastRepository
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ActivityManager _activityManager;

    public WeatherForecastRepository(ActivityManager activityManager)
    {
        _activityManager = activityManager;
    }

    public async Task<IEnumerable<WeatherForecast>> GetAsync(string parentId)
    {
        using var myActivity = _activityManager.StartActivity(nameof(WeatherForecastRepository), ActivityKind.Internal);
        myActivity?.SetParentId(parentId);

        await Task.CompletedTask;
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }
}
