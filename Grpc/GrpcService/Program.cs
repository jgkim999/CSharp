using GrpcService.Services;
using Serilog;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Serilog
        // https://github.com/datalust/dotnet6-serilog-example?ref=blog.datalust.co
        builder.Host.UseSerilog((ctx, lc) => lc
            .WriteTo.Console()
            .ReadFrom.Configuration(ctx.Configuration));

        // https://learn.microsoft.com/ko-kr/aspnet/core/grpc/performance?view=aspnetcore-7.0
        // 흐름 제어
        // HTTP / 2 흐름 제어는 앱이 데이터로 과부하되는 것을 방지하는 기능입니다. 흐름 제어를 사용하는 경우:
        // 각 HTTP/2 연결 및 요청에는 사용 가능한 버퍼 창이 있습니다. 버퍼 창은 앱이 한 번에 받을 수 있는 데이터의 양입니다.
        // 버퍼 창이 채워지면 흐름 제어가 활성화됩니다. 활성화되면 전송 앱이 더 많은 데이터 전송을 일시 중지합니다.
        // 수신 앱이 데이터를 처리하면 버퍼 창의 공간을 사용할 수 있습니다. 전송 앱이 데이터 보내기를 다시 시작합니다.
        // 흐름 제어는 큰 메시지를 수신할 때 성능에 부정적인 영향을 줄 수 있습니다. 버퍼 창이 들어오는 메시지 페이로드보다 작거나 클라이언트와 서버 간에 대기 시간이 있는 경우 시작/중지 버스트에서 데이터를 보낼 수 있습니다.
        // 흐름 제어 성능 문제는 버퍼 창 크기를 늘려 해결할 수 있습니다. Kestrel에서 이는 앱 시작 시 InitialConnectionWindowSize 및 InitialStreamWindowSize로 구성됩니다.
        /*
        builder.WebHost.ConfigureKestrel(options =>
        {
            var http2 = options.Limits.Http2;
            http2.InitialConnectionWindowSize = 1024 * 1024 * 2; // 2 MB
            http2.InitialStreamWindowSize = 1024 * 1024; // 1 MB
        });
        */
        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

        // Add services to the container.
        builder.Services.AddGrpc(options =>
        {
            // options
            // https://learn.microsoft.com/ko-kr/aspnet/core/grpc/configuration?view=aspnetcore-7.0
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = 2 * 1024 * 1024; // 2 MB
            options.MaxSendMessageSize = 5 * 1024 * 1024; // 5 MB
            // iterceptor
            // https://learn.microsoft.com/ko-kr/aspnet/core/grpc/interceptors?view=aspnetcore-7.0
            options.Interceptors.Add<ServerLoggerInterceptor>();
        });

        builder.Services.AddSingleton<ServerLoggerInterceptor>();

        // https://learn.microsoft.com/ko-kr/aspnet/core/grpc/test-tools?view=aspnetcore-7.0
        builder.Services.AddGrpcReflection();

        var app = builder.Build();
        // Serilog
        // https://github.com/datalust/dotnet6-serilog-example?ref=blog.datalust.co
        app.UseSerilogRequestLogging();

        // Configure the HTTP request pipeline.
        // GreeterService inherits from the GreeterBase type, which is generated from the Greeter service in the .proto file. The service is made accessible to clients in Program.cs:
        app.MapGrpcService<GreeterService>();
        app.MapGrpcService<ExampleService>();
        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        // https://learn.microsoft.com/ko-kr/aspnet/core/grpc/test-tools?view=aspnetcore-7.0
        IWebHostEnvironment env = app.Environment;
        if (env.IsDevelopment())
        {
            app.MapGrpcReflectionService();
        }

        app.Run();
    }
}