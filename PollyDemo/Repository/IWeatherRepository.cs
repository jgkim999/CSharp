using System.Collections;

namespace PollyDemo.Repository;

public interface IWeatherRepository
{
    Task<List<WeatherForecast>> GetAsync();
}