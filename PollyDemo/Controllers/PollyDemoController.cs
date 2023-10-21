using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Registry;
using Polly.Retry;
using Polly.Timeout;
using PollyDemo.Repository;

namespace PollyDemo.Controllers;

[Route("api/v1/[controller]/[action]")]
[ApiController]
public class PollyDemoController : ControllerBase
{
    private readonly ILogger<PollyDemoController> _logger;
    private readonly IWeatherRepository _weatherRepo;
    private readonly ResiliencePipeline _pipeline;
    private readonly ResiliencePipeline _retryTimeout;
    private readonly ResiliencePipelineRegistry<string> _registry;
    public PollyDemoController(
        ILogger<PollyDemoController> logger,
        IWeatherRepository weatherRepo,
        ResiliencePipelineRegistry<string> registry)
    {
        _logger = logger;
        _weatherRepo = weatherRepo;
        _registry = registry;
        _pipeline = registry.GetPipeline("retry");
        _retryTimeout = registry.GetPipeline("retry-timeout");
        /*
        var pipelineProvider = HttpContext.RequestServices.GetService<ResiliencePipelineProvider<string>>();
        _pipeline = pipelineProvider.GetPipeline("my-key");
        */
        /*
        var provider = serviceCollection.BuildServiceProvider();
        var pipelineProvider = provider.GetRequiredService<ResiliencePipelineProvider<string>>();
        _pipeline = pipelineProvider.GetPipeline("my-key");
        */
        /*
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(), // Exception이 발생하면
                Delay = TimeSpan.FromSeconds(1), // 1초 쉬고 재시도
                MaxRetryAttempts = 3, // 3번까지 재시도
                BackoffType = DelayBackoffType.Linear // 재시도 시간 선형증가, 1초 -> 2초 -> 3초
            })
            .Build(); // After all necessary strategies are added, call Build() to create the pipeline.
        */
    }
    
    // GET: api/PollyDemo
    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        CancellationToken token = new CancellationToken();
        var weathers = await _retryTimeout.ExecuteAsync<IEnumerable<WeatherForecast>>(async token =>
        {
            return await _weatherRepo.GetAsync();
        }, token);
        return weathers;
    }

    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> Get2()
    {
        var pipeline = _registry.GetOrAddPipeline("xxx", (builder, context) =>
        {
            builder.AddRetry(new RetryStrategyOptions()
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder().Handle<Exception>()
            }).AddTimeout(
                new TimeoutStrategyOptions()
                {
                    Timeout = TimeSpan.FromSeconds(1)
                }).Build();
        });
        var weathers = await pipeline.ExecuteAsync<IEnumerable<WeatherForecast>>(async token =>
        {
            return await _weatherRepo.GetAsync();
        }, CancellationToken.None);
        return weathers;
    }
}