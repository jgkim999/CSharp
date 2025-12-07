namespace Demo.Application.Utils;

public static class MqName
{
    public static string MultiExchange(string role)
    {
        return $"{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.{role}.M";
    }
    
    public static string MultiQueue(string role)
    {
        return $"{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.{role}.M.{Ulid.NewUlid()}";
    }

    public static string AnyQueue(string role)
    {
        return $"{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.{role}.A";
    }
    
    public static string UniqueQueue(string role)
    {
        return $"{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.{role}.U.{Ulid.NewUlid()}";
    }
}
