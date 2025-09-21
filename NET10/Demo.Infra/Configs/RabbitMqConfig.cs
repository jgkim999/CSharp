namespace Demo.Infra.Configs;

public class RabbitMqConfig
{
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ExchangePrefix { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}
