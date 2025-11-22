using Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ 설정
var rabbitUser = builder.AddParameter("rabbitmq-user");
var rabbitPassword = builder.AddParameter("rabbitmq-password", secret: true);
var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPassword)
    .WithManagementPlugin();

// Valkey 설정 (인증 없이 접속)
var valkey = builder.AddContainer("valkey", "valkey/valkey")
    .WithImageTag("8.0-alpine")
    .WithEndpoint(port: 6379, targetPort: 6379, name: "tcp")
    .WithArgs("valkey-server", "--requirepass", "", "--protected-mode", "no");

// PostgreSQL 설정
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var postgresServer = builder.AddPostgres("postgres", password: postgresPassword)
    .WithPgAdmin(c => c
        .WithImageTag("9.10")   // 최신 PgAdmin 버전 사용
        .WithHostPort(5050));   // PgAdmin 포트를 고정하여 접근성 향상

var postgres = postgresServer.AddDatabase("mydatabase");

// MySQL 설정
var mysqlPassword = builder.AddParameter("mysql-password", secret: true);
var mysqlServer = builder.AddMySql("mysql", password: mysqlPassword)
    .WithDataVolume("mysql-data")   // 영구 볼륨으로 데이터 저장
    .WithBindMount("./mysql-init", "/docker-entrypoint-initdb.d")  // 초기화 스크립트 마운트
    .WithPhpMyAdmin(c => c
        .WithHostPort(8080));   // phpMyAdmin 포트

var mysqlDatabase = mysqlServer.AddDatabase("mydb");

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
var postgresEndpoint = postgresServer.GetEndpoint("tcp");
var mysqlEndpoint = mysqlServer.GetEndpoint("tcp");

// Demo.Web
builder.AddProject<Demo_Web>("demo-web")
       .WithReference(rabbitmq)
       .WithReference(postgres)
       .WithReference(mysqlDatabase)
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
       .WithEnvironment("Postgres__ConnectionString", $"Host={postgresEndpoint.Property(EndpointProperty.Host)};Port={postgresEndpoint.Property(EndpointProperty.Port)};Database=mydatabase;Username=postgres;Password={postgresPassword};Maximum Pool Size=8;Minimum Pool Size=2;")
       .WithEnvironment("MySQL__ConnectionString", $"Server={mysqlEndpoint.Property(EndpointProperty.Host)};Port={mysqlEndpoint.Property(EndpointProperty.Port)};Database=mydb;User=root;Password={mysqlPassword};")
       .WaitFor(rabbitmq)        // RabbitMQ가 준비될 때까지 대기
       .WaitFor(valkey)          // Redis 컨테이너가 준비될 때까지 대기
       .WaitFor(postgres)        // PostgreSQL이 준비될 때까지 대기
       .WaitFor(mysqlDatabase);  // MySQL이 준비될 때까지 대기

// Demo.SimpleSocket
builder.AddProject<Demo_SimpleSocket>("demo-simple-socket")
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
       .WithEnvironment("MySQL__ConnectionString", $"Server={mysqlEndpoint.Property(EndpointProperty.Host)};Port={mysqlEndpoint.Property(EndpointProperty.Port)};Database=mydb;User=root;Password={mysqlPassword};")
       .WaitFor(rabbitmq)        // RabbitMQ가 준비될 때까지 대기
       .WaitFor(valkey)          // Redis 컨테이너가 준비될 때까지 대기
       .WaitFor(mysqlDatabase);  // MySQL이 준비될 때까지 대기
builder.Build().Run();
