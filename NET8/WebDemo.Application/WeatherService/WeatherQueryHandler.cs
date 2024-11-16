using MediatR;
using WebDemo.Application.Repositories;
using WebDemo.Domain.Models;

namespace WebDemo.Application.WeatherService;

public class WeatherRequest : IRequest<IEnumerable<WeatherForecast>>
{
}

public class WeatherQueryHandler : IRequestHandler<WeatherRequest, IEnumerable<WeatherForecast>>
{
    private readonly IWeatherForecastRepository _repo;
    private readonly ActivityManager _activityManager;

    public WeatherQueryHandler(IWeatherForecastRepository repo, ActivityManager activityManager)
    {
        _repo = repo;
        _activityManager = activityManager;
    }

    public async Task<IEnumerable<WeatherForecast>> Handle(WeatherRequest request, CancellationToken cancellationToken)
    {
        using var myActivity = _activityManager.StartActivity(nameof(WeatherQueryHandler));
        return await _repo.GetAsync();
    }
}
