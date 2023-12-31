using RabbitMQ.Client;

namespace DemoApplication.Mq;

public class MqBase
{
    readonly IConnection _connection;
    protected readonly IModel Channel;

    protected MqBase(ConnectionFactory factory)
    {
        _connection = factory.CreateConnection();
        Channel = _connection.CreateModel();
    }

    protected void CreateQueueIfNotExists(string queueName)
    {
        Channel.QueueDeclare(queueName, autoDelete: false, exclusive: false, durable: true, arguments: new Dictionary<string, object>());
    }

    ~MqBase()
    {
        _connection.Dispose();
        Channel.Dispose();
    }
}