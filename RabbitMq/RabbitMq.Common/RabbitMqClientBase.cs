using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace RabbitMq.Common;

public abstract class RabbitMqClientBase : IDisposable
{
    protected const string VirtualHost = "CUSTOM_HOST";
    protected const string LoggerQueueAndExchangeRoutingKey = "log.message";
    private readonly ConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqClientBase> _logger;
    protected readonly string LoggerExchange = $"{VirtualHost}.LoggerExchange";
    protected readonly string LoggerQueue = $"{VirtualHost}.log.message";
    private IConnection _connection;

    protected RabbitMqClientBase(
        ConnectionFactory connectionFactory,
        ILogger<RabbitMqClientBase> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        ConnectToRabbitMq();
    }

    protected IModel Channel { get; private set; }

    public void Dispose()
    {
        try
        {
            Channel?.Close();
            Channel?.Dispose();
            Channel = null;

            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Cannot dispose RabbitMQ channel or connection");
        }
    }

    private void ConnectToRabbitMq()
    {
        if (_connection == null || _connection.IsOpen == false)
        {
            _connection = _connectionFactory.CreateConnection();
        }

        if (Channel == null || Channel.IsOpen == false)
        {
            Channel = _connection.CreateModel();
            Channel.ExchangeDeclare(LoggerExchange, "direct", true, false);
            Channel.QueueDeclare(LoggerQueue, false, false, false);
            Channel.QueueBind(LoggerQueue, LoggerExchange, LoggerQueueAndExchangeRoutingKey);
        }
    }
}
