using DemoApplication.Interfaces;
using DemoDomain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DemoApplication;

public class WeatherForcastSelect : IRequest<IEnumerable<WeatherForecast>>
{
}

public class WeatherForcastSelectHandler : IRequestHandler<WeatherForcastSelect, IEnumerable<WeatherForecast>>
{
    private readonly ILogger<WeatherForcastSelectHandler> _logger;
    private readonly IWeatherForecastRepository _repo;

    public WeatherForcastSelectHandler(ILogger<WeatherForcastSelectHandler> logger, IWeatherForecastRepository repo)
    {
        _logger = logger;
        _repo = repo;
    }
    
    public async Task<IEnumerable<WeatherForecast>> Handle(WeatherForcastSelect request, CancellationToken cancellationToken)
    {
        return await _repo.SelectAsync();
    }
}