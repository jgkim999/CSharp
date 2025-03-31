using FluentResults;

using StackExchange.Redis;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Demo.Infra.Extentions;

public static class RedisDatabaseExtentions
{
    private static JsonSerializerOptions serializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = null,
        WriteIndented = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task SetJsonAsync<T>(this IDatabase cache, string key, T value)
    {
        string json = JsonSerializer.Serialize(value, serializerOptions);
        await cache.StringSetAsync(key, json, TimeSpan.FromMinutes(1));
    }

    public static async Task<Result<T>> GetAsync<T>(this IDatabase cache, string key)
    {
        try
        {
            RedisValue val = await cache.StringGetAsync(key);
            if (!val.HasValue)
                return Result.Fail("No value");
            T value = JsonSerializer.Deserialize<T>(val, serializerOptions);
            return value;
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }
    }
}
