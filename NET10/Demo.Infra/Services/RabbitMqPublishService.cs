using System.Diagnostics;
using System.Text;
using Demo.Domain;
using Demo.Infra.Configs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using Demo.Application.Services;

namespace Demo.Infra.Services;

public class RabbitMqPublishService : IMqPublishService, IDisposable
{
    private readonly string _hostName;
    private readonly string _queueName;
    private readonly RabbitMqConfig _config;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of <see cref="RabbitMqPublishService"/> using the provided RabbitMQ configuration and telemetry service.
    /// </summary>
    /// <remarks>
    /// Validates inputs and stores configuration values for the host and queue. Creates a RabbitMQ connection and channel synchronously (blocking) and declares the configured queue with durable=false, exclusive=false and autoDelete=true.
    /// Note: the queue declaration is initiated via <c>QueueDeclareAsync</c> but is not awaited here; any exceptions from connection/channel creation or queue declaration will propagate to the caller.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <c>config</c> or <c>telemetryService</c> is null.</exception>
    public RabbitMqPublishService(IOptions<RabbitMqConfig> config, ITelemetryService telemetryService)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(telemetryService);
        _config = config.Value;
        _telemetryService = telemetryService;
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
    
    /// <summary>
    /// Releases RabbitMQ resources used by this service by disposing the channel and connection.
    /// </summary>
    /// <remarks>
    /// After calling this method the instance should not be used to publish messages.
    /// </remarks>
    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// Publishes the provided text message to the configured RabbitMQ queue and records a producer telemetry activity.
    /// </summary>
    /// <param name="message">The UTF-8 text payload to publish to the configured queue. The payload is encoded as UTF-8 and sent as the message body.</param>
    /// <remarks>
    /// - Injects the current OpenTelemetry trace context into the message headers (header values stored as UTF-8 byte arrays) so downstream consumers can continue the trace.
    /// - Records a producer activity named "rabbitmq.publish" with messaging attributes and marks the activity successful on completion.
    /// - Exceptions from RabbitMQ client operations are not handled here and will propagate to the caller.
    /// </remarks>
    public async ValueTask PublishMessageAsync(string message)
    {
        // Producer Activity 생성
        using var activity = _telemetryService.StartActivity(
            "rabbitmq.publish", 
            ActivityKind.Producer,
            new Dictionary<string, object?>
            {
                ["messaging.system"] = "rabbitmq",
                ["messaging.destination"] = _queueName,
                ["messaging.operation"] = "publish",
                ["messaging.message.payload_size_bytes"] = Encoding.UTF8.GetByteCount(message)
            });
        
        var body = Encoding.UTF8.GetBytes(message);
        
        // BasicProperties를 생성하여 trace context를 헤더에 주입
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>()
        };
        
        // OpenTelemetry trace context를 메시지 헤더에 주입
        var propagator = Propagators.DefaultTextMapPropagator;
        var activityContext = Activity.Current?.Context ?? default;
        
        propagator.Inject(new PropagationContext(activityContext, Baggage.Current), properties.Headers, 
            (headers, key, value) =>
            {
                if (headers != null)
                {
                    headers[key] = Encoding.UTF8.GetBytes(value);
                }
            });
        
        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: _queueName,
            body: body,
            mandatory: false,
            basicProperties: properties);
            
        _telemetryService.SetActivitySuccess(activity, "Message published successfully");
    }
}
