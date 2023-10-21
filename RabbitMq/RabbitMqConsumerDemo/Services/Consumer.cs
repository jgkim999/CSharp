using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsumerDemo1.Services;

public class Consumer: IDisposable
{
    private readonly ILogger<Consumer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public Consumer(ILogger<Consumer> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "user",
            Password = "1234"
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(
            queue: "hello",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public void Receive()
    {
        _logger.LogInformation(" [*] Waiting for messages.");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (ch, ea) =>
        {
            await OnNewMessageReceived(ch, ea);
        };
        _channel.BasicConsume(queue: "hello",
            autoAck: true,
            consumer: consumer);
    }
    
    private async Task OnNewMessageReceived(object sender, BasicDeliverEventArgs e)
    {
        var body = e.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        _logger.LogInformation($" [x] Received {message}");
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Close(Constants.ReplySuccess, "Closing the channel");
        _connection.Close(Constants.ReplySuccess, "Closing the connection");
        _channel.Dispose();
        _connection.Dispose();
    }
}
