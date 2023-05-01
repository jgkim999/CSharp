using Grpc.Net.Client;
using GrpcGreeter;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // 연결 유지 ping
        // 연결 유지 ping을 사용하여 비활성 기간 동안 HTTP/ 2 연결을 유지할 수 있습니다.
        // 앱이 작업을 계속할 때 기존 HTTP/ 2 연결을 곧바로 사용할 수 있으면 연결을 다시 설정하느라 지연이 발생하지 않고 초기 gRPC 호출을 신속하게 수행할 수 있습니다.
        // https://learn.microsoft.com/ko-kr/aspnet/core/grpc/performance?view=aspnetcore-7.0
        var handler = new SocketsHttpHandler
        {
            // 비활성 기간 동안 60초마다 연결 유지 ping을 서버에 보내는 채널을 구성합니다.
            // ping을 사용하면 서버와 사용 중인 모든 프록시가 비활성 상태로 인해 연결을 닫지 않습니다.
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = true
        };

        // The port number must match the port of the gRPC server.
        using var channel = GrpcChannel.ForAddress("https://localhost:7165", new GrpcChannelOptions
        {
            HttpHandler = handler
        });
        var client = new Greeter.GreeterClient(channel);
        var reply = await client.SayHelloAsync(
                          new HelloRequest { Name = "GreeterClient" });
        Console.WriteLine("Greeting: " + reply.Message);
        Console.WriteLine("Press any key to exit...");
        Console.Read();
    }
}
//Console.ReadKey();