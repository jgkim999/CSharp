using System.Diagnostics;
using System.Text;
using Demo.Domain;
using Demo.Infra.Configs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Demo.Infra.Services;

public class RabbitMqPublishService : IMqPublishService, IDisposable
{
    private readonly string _hostName;
    private readonly string _queueName;
    private readonly RabbitMqConfig _config;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMqPublishService(IOptions<RabbitMqConfig> config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config.Value;
        _hostName = _config.HostName;
        _queueName = _config.QueueName;

        var factory = new ConnectionFactory
        {
            //UserName = ConnectionFactory.DefaultUser,
            //Password = ConnectionFactory.DefaultPass,
            //VirtualHost = ConnectionFactory.DefaultVHost,
            HostName = _hostName,
            //Port = AmqpTcpEndpoint.UseDefaultPort,
            //MaxInboundMessageBodySize = 512 * 1024 * 1024
        };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(queue: _queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null);
    }
    
    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }

    public async ValueTask PublishMessageAsync(string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        
        // RabbitMQ instrumentation이 자동으로 trace context를 처리하도록 함
        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: _queueName,
            body: body);
    }
}
