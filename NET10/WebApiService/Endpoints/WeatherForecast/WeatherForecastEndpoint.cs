using FastEndpoints;
using Microsoft.Extensions.Caching.Hybrid;

namespace WebApiService.Endpoints.WeatherForecast;

public class WeatherForecastRes(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class WeatherForecastEndpoint : Endpoint<EmptyRequest, WeatherForecastRes[]>
{
    ILogger<WeatherForecastEndpoint> _logger;
    private HybridCache _cache;
    public WeatherForecastEndpoint(ILogger<WeatherForecastEndpoint> logger, HybridCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public override void Configure()
    {
        Verbs(Http.GET);
        Routes("/wf");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var forecasts = await _cache.GetOrCreateAsync<WeatherForecastRes[]>(
            $"weather-forecast",
            async (ct)  =>
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

        await Send.ResponseAsync(forecasts, cancellation: ct);

        //Response = forecasts;
        //await SendAsync(forecasts, cancellation: ct);
    }
}
