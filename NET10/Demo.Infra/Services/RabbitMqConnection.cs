using Demo.Infra.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Demo.Infra.Services;

public class RabbitMqConnection : IDisposable, IAsyncDisposable
{
    private readonly string _hostName;
    private readonly RabbitMqConfig _config;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    
    public IChannel Channel => _channel;
    
    private readonly string _producerExchangeMulti;
    
    private readonly string _anyQueue;
    
    public string ProducerExchangeMulti => _producerExchangeMulti;

    public string AnyQueue => _anyQueue;
    
    public RabbitMqConnection(IOptions<RabbitMqConfig> config, ILogger<RabbitMqConsumerService> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        _logger = logger;
        _config = config.Value;
        _hostName = _config.HostName;

        var factory = new ConnectionFactory
        {
            //UserName = ConnectionFactory.DefaultUser,
            //Password = ConnectionFactory.DefaultPass,
            //VirtualHost = ConnectionFactory.DefaultVHost,
            HostName = _hostName,
            //Port = AmqpTcpEndpoint.UseDefaultPort,
            //MaxInboundMessageBodySize = 512 * 1024 * 1024
            AutomaticRecoveryEnabled = true
        };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        
        _channel.BasicAcksAsync += (sender, args) =>
        {
            _logger.LogInformation("Message acked");
            return Task.CompletedTask;
        };
        
        _channel.BasicNacksAsync += (sender, args) =>
        {
            _logger.LogInformation("Message nack {@Args}", args);
            return Task.CompletedTask;
        };
        
        _channel.BasicReturnAsync += (sender, args) =>
        {
            _logger.LogInformation("Message return {@Args}", args);
            return Task.CompletedTask;
        };
        
        _channel.CallbackExceptionAsync += (sender, args) =>
        {
            _logger.LogError(args.Exception, "Callback exception");
            return Task.CompletedTask;
        };
        
        _channel.ChannelShutdownAsync += (sender, args) =>
        {
            _logger.LogInformation(args.Exception, "Channel shutdown {@Args}", args);
            return Task.CompletedTask;
        };
        
        _channel.FlowControlAsync += (sender, args) =>
        {
            _logger.LogInformation("FlowControl {@Args}", args);
            return Task.CompletedTask;
        };
        
        _producerExchangeMulti = _config.ExchangePrefix + ".producer@M";
        
        _anyQueue = "demo.any.shared";
        _channel.ExchangeDeclareAsync(exchange: _producerExchangeMulti, type: "fanout");
        
        _channel.QueueDeclareAsync(queue: _anyQueue,
            durable: true,  // 서버 재시작 시에도 유지
            exclusive: false,
            autoDelete: false, // 공유 queue이므로 자동 삭제 비활성화
            arguments: null);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}