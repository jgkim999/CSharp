using System.Text;
using DemoApplication.Settings;
using DemoDomain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using ILogger = Serilog.ILogger;

namespace DemoApplication.Mq;

public class MqConsumer
{
    private readonly ILogger _logger;
    private readonly ConnectionFactory _factory;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    
    public MqConsumer(IOptions<RabbitMqSettings> settings)
    {
        _logger = Log.Logger.ForContext<MqConsumer>();
        
        _factory = new ConnectionFactory()
        {
            HostName = settings.Value.Address,
            Port = settings.Value.Port,
            UserName = settings.Value.User,
            Password = settings.Value.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };
        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(queue: "test-queue", durable: false, exclusive: false, autoDelete: true, arguments: null);
        _channel.QueueBind(queue: "test-queue", exchange: "test", routingKey: "#", arguments: null);
        
        var eventBasicConsumer = new AsyncEventingBasicConsumer(_channel);
        _channel.BasicConsume(
            queue: "test-queue",
            autoAck: true,
            consumer: eventBasicConsumer,
            noLocal: false,
            exclusive: false,
            consumerTag: Guid.NewGuid().ToString(),
            arguments: new Dictionary<string, object>());
        
        eventBasicConsumer.Received += ReceiveAsync;
    }

    private async Task ReceiveAsync(object sender, BasicDeliverEventArgs @event)
    {
        var s = @event.Body.ToArray();
        var postJson = Encoding.UTF8.GetString(s);
        var post = JsonConvert.DeserializeObject<WeatherForecast>(postJson)!;
        _logger.Information($"Consumer {postJson}");
        await Task.CompletedTask;
    }
}