namespace Demo.Application.Configs;

/// <summary>
/// Configuration settings for Redis connections
/// </summary>
public class RedisConfig
{
    /// <summary>
    /// Connection string for JWT token storage in Redis
    /// </summary>
    public string JwtConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Connection string for IP to Nation cache in Redis
    /// </summary>
    public string IpToNationConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix for Redis keys to avoid conflicts
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;
}