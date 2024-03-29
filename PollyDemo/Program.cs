using Polly;
using Polly.Registry;
using Polly.Retry;
using Polly.Timeout;
using PollyDemo.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResiliencePipeline("my-retry", builder =>
{
    builder.AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(), // Exception이 발생하면
        Delay = TimeSpan.FromSeconds(1), // 1초 쉬고 재시도
        MaxRetryAttempts = 3, // 3번까지 재시도
        BackoffType = DelayBackoffType.Linear // 재시도 시간 선형증가, 1초 -> 2초 -> 3초
    });
});

var registry = new ResiliencePipelineRegistry<string>();
registry.TryAddBuilder("retry", ((builder, context) =>
{
    builder.AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(), // Exception이 발생하면
        Delay = TimeSpan.FromSeconds(1), // 1초 쉬고 재시도
        MaxRetryAttempts = 3, // 3번까지 재시도
        BackoffType = DelayBackoffType.Linear, // 재시도 시간 선형증가, 1초 -> 2초 -> 3초
        
    });
}));
registry.TryAddBuilder("timeout", ((builder, context) =>
{
    builder.AddTimeout(new TimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(1)
    });
}));

ResiliencePipeline pipelineY = new ResiliencePipelineBuilder()
    .AddRetry(new()
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Constant,
        ShouldHandle = new PredicateBuilder().Handle<Exception>()
    })
    .AddTimeout(TimeSpan.FromSeconds(1))
    .Build();
registry.TryAddBuilder("retry-timeout", ((builder, context) =>
{
    builder.AddPipeline(pipelineY);
}));

builder.Services.AddSingleton<ResiliencePipelineRegistry<string>>(registry);

// Add services to the container.
builder.Services.AddTransient<IWeatherRepository, WeatherRepository>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();