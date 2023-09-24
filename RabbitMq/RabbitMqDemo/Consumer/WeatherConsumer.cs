using MassTransit;
using RabbitMqDemo.Models;

namespace RabbitMqDemo.Consumer;

public class WeatherConsumer : IConsumer<Weather>
{
    private readonly ILogger<WeatherConsumer> _logger;

    public WeatherConsumer(ILogger<WeatherConsumer> logger)
    {
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<Weather> context)
    {
        _logger.LogInformation("queue1, " + context.Message.City + ":" + context.Message.Celsius);
        await Task.CompletedTask;
    }
}

public class Queue2Consumer : IConsumer<Weather2>
{
    private readonly ILogger<Queue2Consumer> _logger;

    public Queue2Consumer(ILogger<Queue2Consumer> logger)
    {
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<Weather2> context)
    {
        _logger.LogInformation("queue2, " + context.Message.City + ":" + context.Message.Celsius);
        await Task.CompletedTask;
    }
}
