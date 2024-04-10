using RestSharpPractice;

internal class Program
{
    private static void Main(string[] args)
    {
        var restClient = new RestSharpTest();
        var currentTime = restClient.Get().GetAwaiter().GetResult();
        var laTime = restClient.Post(currentTime).GetAwaiter().GetResult();
    }
}