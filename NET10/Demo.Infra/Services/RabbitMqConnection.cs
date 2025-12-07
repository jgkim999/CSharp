using Demo.Application.Utils;
using Demo.Infra.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Demo.Infra.Services;

public class RabbitMqConnection : IDisposable, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    
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
        
        RabbitMqConfig mqConfig = config.Value;
    
        var factory = new ConnectionFactory
        {
            UserName = mqConfig.UserName,
            Password = mqConfig.Password,
            VirtualHost = mqConfig.VirtualHost,
            HostName = mqConfig.HostName,
            Port = mqConfig.Port,
            //MaxInboundMessageBodySize = 512 * 1024 * 1024
            AutomaticRecoveryEnabled = mqConfig.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(mqConfig.NetworkRecoveryInterval),
            TopologyRecoveryEnabled = mqConfig.TopologyRecoveryEnabled,
            ConsumerDispatchConcurrency = mqConfig.ConsumerDispatchConcurrency, // 동시 처리 개수
        };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();

        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        // QoS (Quality of Service) 설정 - Prefetch Count
        // Consumer가 한 번에 받을 수 있는 미확인 메시지 개수 제한
        // 메모리 관리 및 메시지 분산 처리를 위해 필수 설정
        if (mqConfig.PrefetchCount > 0)
        {
            _channel.BasicQosAsync(
                prefetchSize: 0,                     // 0 = 메시지 크기 제한 없음
                prefetchCount: mqConfig.PrefetchCount, // 한 번에 받을 메시지 개수
                global: false                        // false = 각 Consumer마다 적용, true = Channel 전체 적용
            ).ConfigureAwait(false).GetAwaiter().GetResult();

            logger.LogInformation(
                "BasicQos configured - PrefetchCount: {PrefetchCount}, ConsumerDispatchConcurrency: {Concurrency}",
                mqConfig.PrefetchCount, mqConfig.ConsumerDispatchConcurrency);
        }
        else
        {
            logger.LogWarning("PrefetchCount is 0 (unlimited) - This may cause memory issues with large message queues!");
        }
        // RabbitMQ.Client 7.x에서는 publisher confirms가 기본적으로 활성화됨

        _channel.BasicAcksAsync += (sender, args) =>
        {
            logger.LogDebug("Message acked {Sender} {Args}", sender, args);
            return Task.CompletedTask;
        };
        
        _channel.BasicNacksAsync += (sender, args) =>
        {
            logger.LogDebug("Message nack {@Sender} {@Args}", sender, args);
            return Task.CompletedTask;
        };
        
        _channel.BasicReturnAsync += (sender, args) =>
        {
            logger.LogDebug("Message return {@Sender} {@Args}", sender, args);
            return Task.CompletedTask;
        };
        
        _channel.CallbackExceptionAsync += (sender, args) =>
        {
            logger.LogError(args.Exception, "Callback exception {@Sender} {@Args}", sender, args);
            return Task.CompletedTask;
        };
        
        _channel.ChannelShutdownAsync += (sender, args) =>
        {
            logger.LogInformation(args.Exception, "Channel shutdown {@Sender} {@Args}", sender, args);
            return Task.CompletedTask;
        };
        
        _channel.FlowControlAsync += (sender, args) =>
        {
            logger.LogInformation("FlowControl {@Sender} {@Args}", sender, args);
            return Task.CompletedTask;
        };
        
        // Multi
        // Exchange에 연결된 모든 Queue에 전달 
        // Exchange -> Queue1 -> Consumer1 (1) (2) (3)
        //          -> Queue2 -> Consumer2 (1) (2) (3)
        //          -> Queue3 -> Consumer3 (1) (2) (3)
        _multiExchange = MqName.MultiExchange(mqConfig.Role);
        _multiQueue = MqName.MultiQueue(mqConfig.Role);

        // Any
        // Queue에 연결된 Consumer에 라운드로빈 방식으로 전달
        // Queue1 -> Consumer1 (1) (4)
        //       -> Consumer2 (2) (5)
        //       -> Consumer3 (3) (6)
        _anyQueue = MqName.AnyQueue(mqConfig.Role);
        
        // Unique
        // 정확한 Queue이름으로 전달
        // Queue1 -> Consumer1
        // Queue2 -> Consumer2
        // Queue3 -> Consumer3
        _unqueQueue = MqName.UniqueQueue(mqConfig.Role);
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