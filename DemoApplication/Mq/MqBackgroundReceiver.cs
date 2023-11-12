using DemoApplication.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DemoApplication.Mq;

public class MqBackgroundReceiver : BackgroundService
{
    private readonly ILogger<MqBackgroundReceiver> _logger;
    private readonly IOptions<RabbitMqSettings> _settings;
    private MqConsumer? _consumer;

    public MqBackgroundReceiver(
        ILogger<MqBackgroundReceiver> logger,
        IOptions<RabbitMqSettings> settings)
    {
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer = new MqConsumer(_settings);
        await Task.CompletedTask;
    }
    
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Consume Scoped Service Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }
}