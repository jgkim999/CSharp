namespace Demo.Infra.Configs;

public class RabbitMqConfig
{
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    
    public ushort ConsumerDispatchConcurrency { get; set; } = 1;
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public long NetworkRecoveryInterval { get; set; } = 5;
    public bool TopologyRecoveryEnabled { get; set; } = true;
    
    public string MultiExchange { get; set; } = string.Empty;
    public string MultiQueue { get; set; } = string.Empty;
    
    public string AnyQueue { get; set; } = string.Empty;
    
    public string UniqueQueue { get; set; } = string.Empty;
}
