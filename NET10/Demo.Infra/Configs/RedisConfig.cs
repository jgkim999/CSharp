namespace Demo.Infra.Configs;

/// <summary>
/// 
/// </summary>
public class RedisConfig
{
    /// <summary>
    /// 
    /// </summary>
    public string JwtConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string IpToNationConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;
}
