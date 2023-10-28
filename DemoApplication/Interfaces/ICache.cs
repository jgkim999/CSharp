namespace DemoApplication.Interfaces;

public interface ICache
{
    T? Get<T>(string key);
    Task<T?> GetAsync<T>(string key);
    void Set<T>(string key, T value, TimeSpan expirationTime);
    Task SetAsync<T>(string key, T value, TimeSpan expirationTime);
    void Remove(string key);
    Task RemoveAsync(string key);
}
