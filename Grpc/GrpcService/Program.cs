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
        // �帧 ����
        // HTTP / 2 �帧 ����� ���� �����ͷ� �����ϵǴ� ���� �����ϴ� ����Դϴ�. �帧 ��� ����ϴ� ���:
        // �� HTTP/2 ���� �� ��û���� ��� ������ ���� â�� �ֽ��ϴ�. ���� â�� ���� �� ���� ���� �� �ִ� �������� ���Դϴ�.
        // ���� â�� ä������ �帧 ��� Ȱ��ȭ�˴ϴ�. Ȱ��ȭ�Ǹ� ���� ���� �� ���� ������ ������ �Ͻ� �����մϴ�.
        // ���� ���� �����͸� ó���ϸ� ���� â�� ������ ����� �� �ֽ��ϴ�. ���� ���� ������ �����⸦ �ٽ� �����մϴ�.
        // �帧 ����� ū �޽����� ������ �� ���ɿ� �������� ������ �� �� �ֽ��ϴ�. ���� â�� ������ �޽��� ���̷ε庸�� �۰ų� Ŭ���̾�Ʈ�� ���� ���� ��� �ð��� �ִ� ��� ����/���� ����Ʈ���� �����͸� ���� �� �ֽ��ϴ�.
        // �帧 ���� ���� ������ ���� â ũ�⸦ �÷� �ذ��� �� �ֽ��ϴ�. Kestrel���� �̴� �� ���� �� InitialConnectionWindowSize �� InitialStreamWindowSize�� �����˴ϴ�.
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