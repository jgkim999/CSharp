namespace Demo.Infra.Config;

class RabbitMqConfig
{
    public string Host { get; }
    public int Port { get; }
    public string Username { get; }
    public string Password { get; }
    public string VirtualHost { get; }
    public string Exchange { get; }
    public string Queue { get; }
    public string RoutingKey { get; }
}
