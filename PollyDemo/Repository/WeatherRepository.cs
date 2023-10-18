namespace PollyDemo.Repository;

public class WeatherRepository : IWeatherRepository
{
    private readonly ILogger<WeatherRepository> _logger;
    private static Random _rand = new Random(DateTime.Now.Nanosecond);

    public WeatherRepository(ILogger<WeatherRepository> logger)
    {
        _logger = logger;
    }
    
    public async Task<List<WeatherForecast>> GetAsync()
    {
        await Task.CompletedTask;
        var randValue = _rand.Next(0, 2);
        if (randValue == 1)
        {
            _logger.LogInformation("Success.");
            List<WeatherForecast> weathers = new();
            weathers.Add(new WeatherForecast()
            {
                TemperatureC = _rand.Next(-10, 30),
                Date = DateOnly.FromDateTime(DateTime.Now)
            });
            return weathers;
        }
        else
        {
            _logger.LogError("Failed.");
            throw new SystemException("Something wrong.");    
        }
    }
}