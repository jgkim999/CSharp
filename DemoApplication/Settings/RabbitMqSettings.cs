namespace DemoApplication.Settings;

public class RabbitMqSettings
{
    public string Address { get; set; } = string.Empty;
    
    public int Port { get; set; }

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
