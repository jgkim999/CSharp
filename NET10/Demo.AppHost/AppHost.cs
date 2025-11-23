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

// Demo.Admin
builder.AddProject<Demo_Admin>("demo-admin")
    .WithReference(rabbitmq)
    .WithReference(postgres)
    .WithReference(mysqlDatabase)
    .WithRabbitMqEnvironment(rabbitmq, rabbitUser, rabbitPassword)
    .WithRedisEnvironment(valkey)
    .WithPostgresEnvironment(postgresServer, postgresPassword)
    .WithMySqlEnvironment(mysqlServer, mysqlPassword)
    .WaitForInfrastructure(rabbitmq, valkey, postgres, mysqlDatabase);

// Demo.Consumer
builder.AddProject<Demo_Consumer>("demo-consumer")
    .WithReference(rabbitmq)
    .WithReference(postgres)
    .WithReference(mysqlDatabase)
    .WithRabbitMqEnvironment(rabbitmq, rabbitUser, rabbitPassword)
    .WithRedisEnvironment(valkey)
    .WithPostgresEnvironment(postgresServer, postgresPassword)
    .WithMySqlEnvironment(mysqlServer, mysqlPassword)
    .WaitForInfrastructure(rabbitmq, valkey, postgres, mysqlDatabase);

// Demo.Web
builder.AddProject<Demo_Web>("demo-web")
    .WithReference(rabbitmq)
    .WithReference(postgres)
    .WithReference(mysqlDatabase)
    .WithRabbitMqEnvironment(rabbitmq, rabbitUser, rabbitPassword)
    .WithRedisEnvironment(valkey)
    .WithPostgresEnvironment(postgresServer, postgresPassword)
    .WithMySqlEnvironment(mysqlServer, mysqlPassword)
    .WaitForInfrastructure(rabbitmq, valkey, postgres, mysqlDatabase);

// Demo.SimpleSocket
builder.AddProject<Demo_SimpleSocket>("demo-simple-socket")
    .WithReference(rabbitmq)
    .WithRabbitMqEnvironment(rabbitmq, rabbitUser, rabbitPassword)
    .WithRedisEnvironment(valkey)
    .WithMySqlEnvironment(mysqlServer, mysqlPassword)
    .WaitForInfrastructure(rabbitmq, valkey, mysql: mysqlDatabase);
builder.Build().Run();
