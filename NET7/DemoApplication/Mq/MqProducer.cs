using System.Text;
using DemoApplication.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DemoApplication.Mq;

public class MqProducer
{
    private readonly ConnectionFactory _factory;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<MqProducer> _logger;
    
    public MqProducer(
        ILogger<MqProducer> logger,
        IOptions<RabbitMqSettings> settings)
    {
        _factory = new ConnectionFactory()
        {
            HostName = settings.Value.Address,
            Port = settings.Value.Port,
            UserName = settings.Value.User,
            Password = settings.Value.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        _logger = logger;
        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(
            exchange: "test",
            type: "topic",
            durable: false,
            autoDelete: false, 
            arguments: null);
    }
    
    public void Publish(string exchange, string routingKey, string msg)
    {
        Publish(exchange, routingKey, Encoding.UTF8.GetBytes(msg));
    }
    public void Publish(string exchange, string routingKey, ReadOnlyMemory<byte> msg)
    {
        _channel.BasicPublish(exchange: exchange, routingKey: routingKey, body: msg);
    }
}