using DemoDomain.Entities;

namespace DemoApplication.Interfaces;

public interface IWeatherForecastRepository
{
    Task<IEnumerable<WeatherForecast>> SelectAsync();
}