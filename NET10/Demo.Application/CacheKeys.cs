namespace Demo.Application;

public static class CacheKeys
{
    public static string UserInfoKey(string email)
    {
        return $"user:info:{email}";
    }
}
