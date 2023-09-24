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
        _logger.LogInformation(context.Message.City + ":" + context.Message.Celsius);
        await Task.CompletedTask;
    }
}