using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Demo.Application.WeatherForecast;

public sealed record WeatherForecastCommand : ICommand<WeatherForecastRes[]>;

public class WeatherForecastHandler : ICommandHandler<WeatherForecastCommand, WeatherForecastRes[]>
{
    ILogger<WeatherForecastHandler> _logger;
    private readonly HybridCache _cache;

    public WeatherForecastHandler(ILogger<WeatherForecastHandler> logger, HybridCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task<WeatherForecastRes[]> HandleAsync(WeatherForecastCommand command, CancellationToken ct)
    {
        var forecasts = await _cache.GetOrCreateAsync<WeatherForecastRes[]>(
            $"weather-forecast",
            async (ct) =>
            {
                var forecasts = Enumerable.Range(1, 5)
                    .Select(index => new WeatherForecastRes(DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        Random.Shared.Next(-20, 55), "Hot" // Placeholder, you can generate more varied summaries
                    ))
                    .ToArray();
                return forecasts;
            },
            new HybridCacheEntryOptions()
            {
                Expiration = TimeSpan.FromSeconds(10)
            },
            cancellationToken: ct);

        _logger.LogDebug("Weather forecast retrieved.");

        return forecasts;
    }
}
