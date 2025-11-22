using Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ 설정
var rabbitUser = builder.AddParameter("rabbitmq-user");
var rabbitPassword = builder.AddParameter("rabbitmq-password", secret: true);
var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPassword)
    .WithManagementPlugin();

// Redis 설정 (인증 없이 접속)
var valkey = builder.AddContainer("valkey", "redis")
    .WithImageTag("7-alpine")
    .WithEndpoint(port: 6379, targetPort: 6379, name: "tcp")
    .WithArgs("redis-server", "--requirepass", "", "--protected-mode", "no");

// Prometheus 추가 (OTel Collector 이전에 실행)
var prometheus = builder.AddContainer("prometheus", "prom/prometheus")
    .WithImageTag("latest")
    .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml")  // 설정 파일 마운트
    .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "web")          // Prometheus Web UI
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
              "--storage.tsdb.path=/prometheus",
              "--web.console.libraries=/usr/share/prometheus/console_libraries",
              "--web.console.templates=/usr/share/prometheus/consoles",
              "--web.enable-lifecycle",                                    // API로 설정 리로드 가능
              "--web.enable-remote-write-receiver");                       // Remote Write 수신 활성화

// RabbitMQ 실제 연결 정보 가져오기
var rabbitMqEndpoint = rabbitmq.GetEndpoint("tcp");
var valkeyEndpoint = valkey.GetEndpoint("tcp");

builder.AddProject<Projects.Demo_Web>("demo-web")
       .WithReference(rabbitmq)
       .WithEnvironment("RabbitMQ__HostName", rabbitMqEndpoint.Property(EndpointProperty.Host))
       .WithEnvironment("RabbitMQ__Port", rabbitMqEndpoint.Property(EndpointProperty.Port))
       .WithEnvironment("RabbitMQ__UserName", rabbitUser)
       .WithEnvironment("RabbitMQ__Password", rabbitPassword)
       .WithEnvironment("RabbitMQ__VirtualHost", "/")
       .WithEnvironment("RabbitMQ__UseSsl", "false")
       .WithEnvironment("RabbitMQ__UseTls", "false")
       .WithEnvironment("RabbitMQ__UseTlsCertificateValidation", "false")
       .WithEnvironment("Redis__JwtConnectionString", $"{valkeyEndpoint.Property(EndpointProperty.Host)}:{valkeyEndpoint.Property(EndpointProperty.Port)},allowAdmin=true,abortConnect=false")
       .WithEnvironment("Redis__IpToNationConnectionString", $"{valkeyEndpoint.Property(EndpointProperty.Host)}:{valkeyEndpoint.Property(EndpointProperty.Port)},allowAdmin=true,abortConnect=false")
       .WaitFor(rabbitmq)        // RabbitMQ가 준비될 때까지 대기
       .WaitFor(valkey);         // Redis 컨테이너가 준비될 때까지 대기

builder.Build().Run();
