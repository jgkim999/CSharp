using System.Diagnostics;

using WebDemo.Domain.Models;

namespace WebDemo.Application.Repositories;

public interface IWeatherForecastRepository
{
    public Task<IEnumerable<WeatherForecast>> GetAsync();
}
