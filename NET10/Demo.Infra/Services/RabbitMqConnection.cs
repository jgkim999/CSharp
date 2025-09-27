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
    
    private readonly string _multiExchange;
    private readonly string _multiQueue;
    
    private readonly string _anyQueue;

    private readonly string _unqueQueue;
    
    public string MultiExchange => _multiExchange;
    
    public string MultiQueue => _multiQueue;

    public string AnyQueue => _anyQueue;
    
    public string UniqueQueue => _unqueQueue;
    
    public RabbitMqConnection(IOptions<RabbitMqConfig> config, ILogger<RabbitMqConsumerService> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        _logger = logger;
        _config = config.Value;
        _hostName = _config.HostName;

        var factory = new ConnectionFactory
        {
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,
            HostName = _config.HostName,
            Port = _config.Port,
            //MaxInboundMessageBodySize = 512 * 1024 * 1024
            AutomaticRecoveryEnabled = _config.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(_config.NetworkRecoveryInterval),
            TopologyRecoveryEnabled = _config.TopologyRecoveryEnabled,
            ConsumerDispatchConcurrency = _config.ConsumerDispatchConcurrency, // 1개씩만 처리하고 나머지는 큐에 넣어두고 처리한다.
        };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();

        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        // Note: RabbitMQ.Client 7.x에서는 publisher confirms가 기본적으로 활성화됨
        
        _channel.BasicAcksAsync += (sender, args) =>
        {
            _logger.LogDebug("Message acked");
            return Task.CompletedTask;
        };
        
        _channel.BasicNacksAsync += (sender, args) =>
        {
            _logger.LogDebug("Message nack {@Args}", args);
            return Task.CompletedTask;
        };
        
        _channel.BasicReturnAsync += (sender, args) =>
        {
            _logger.LogDebug("Message return {@Args}", args);
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
        
        _multiExchange = _config.MultiExchange + "@M";
        
        _multiQueue = _config.MultiQueue + "." + Ulid.NewUlid();

        _anyQueue = _config.AnyQueue;
        
        _unqueQueue = _config.UniqueQueue + "." + Ulid.NewUlid();
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