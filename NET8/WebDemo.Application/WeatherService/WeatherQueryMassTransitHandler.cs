﻿using MassTransit;
using MassTransit.Courier.Contracts;

using MediatR;

using System.Diagnostics;

using WebDemo.Application.Repositories;
using WebDemo.Domain.Models;

namespace WebDemo.Application.WeatherService;

public class WeatherMassTransitRequest : IRequest<IEnumerable<WeatherForecast>>
{
    public string TraceId { get; set; }

    public WeatherMassTransitRequest(string traceId)
    {
        TraceId = traceId;
    }
}

public class WeatherRabbitMqRequest
{
    public string TraceId { get; set; }

    public WeatherRabbitMqRequest(string traceId)
    {
        TraceId = traceId;
    }
}

public class WeatherRabbitMqResponse
{
    public List<WeatherForecast> WeatherForecasts { get; set; }
}

public class WeatherQueryMassTransitHandler :
    IRequestHandler<WeatherMassTransitRequest, IEnumerable<WeatherForecast>>
{
    private readonly ActivityManager _activityManager;
    private readonly IRequestClient<WeatherRabbitMqRequest> _client;

    public WeatherQueryMassTransitHandler(
        ActivityManager activityManager,
        IRequestClient<WeatherRabbitMqRequest> client,
        IBus bus)
    {
        _activityManager = activityManager;
        _client = client;
    }

    public async Task<IEnumerable<WeatherForecast>> Handle(WeatherMassTransitRequest request, CancellationToken cancellationToken)
    {
        using var myActivity = _activityManager.StartActivity(nameof(WeatherQueryMassTransitHandler), ActivityKind.Producer, request.TraceId);

        WeatherRabbitMqRequest req = new WeatherRabbitMqRequest(request.TraceId);
        // _bus.Request<WeatherRabbitMqRequest, IEnumerable<WeatherForecast>>(req, cancellationToken);
        var response = await _client.GetResponse<WeatherRabbitMqResponse>(req, cancellationToken, TimeSpan.FromSeconds(5));
        return response.Message.WeatherForecasts;
    }
}

public class WeatherRabbitMqConsumer : IConsumer<WeatherRabbitMqRequest>
{
    private readonly IWeatherForecastRepository _repo;
    private readonly ActivityManager _activityManager;

    public WeatherRabbitMqConsumer(IWeatherForecastRepository repo, ActivityManager activityManager)
    {
        _repo = repo;
        _activityManager = activityManager;
    }

    public async Task Consume(ConsumeContext<WeatherRabbitMqRequest> context)
    {
        GlobalLogger.GetLogger<WeatherRabbitMqConsumer>().Information("TraceId:{traceId}", context.Message.TraceId);

        using var activity = _activityManager.StartActivity(nameof(WeatherRabbitMqConsumer), ActivityKind.Server, context.Message.TraceId);

        var x = await _repo.GetAsync();
        await context.RespondAsync(new WeatherRabbitMqResponse()
        {
            WeatherForecasts = x.ToList()
        });
    }
}