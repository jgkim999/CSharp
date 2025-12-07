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

// MySQL 설정
var mysqlPassword = builder.AddParameter("mysql-password", secret: true);
var mysqlServer = builder.AddMySql("mysql", password: mysqlPassword)
    .WithDataVolume("mysql-data")   // 영구 볼륨으로 데이터 저장
    .WithBindMount("./mysql-init", "/docker-entrypoint-initdb.d")  // 초기화 스크립트 마운트
    .WithPhpMyAdmin(c => c
        .WithHostPort(8080));   // phpMyAdmin 포트

var mysqlDatabase = mysqlServer.AddDatabase("mydb");

// Demo.Admin
builder.AddProject<Demo_Admin>("demo-admin")
    .WithReference(rabbitmq)
    .WithReference(mysqlDatabase)
    .WithRabbitMqEnvironment(rabbitmq, rabbitUser, rabbitPassword)
    .WithRedisEnvironment(valkey)
    .WithMySqlEnvironment(mysqlServer, mysqlPassword)
    .WaitForInfrastructure(rabbitmq, valkey, mysqlDatabase);

// Demo.Consumer
builder.AddProject<Demo_Consumer>("demo-consumer")
    .WithReference(rabbitmq)
    .WithReference(mysqlDatabase)
    .WithRabbitMqEnvironment(rabbitmq, rabbitUser, rabbitPassword)
    .WithRedisEnvironment(valkey)
    .WithMySqlEnvironment(mysqlServer, mysqlPassword)
    .WaitForInfrastructure(rabbitmq, valkey, mysqlDatabase);

// Demo.SimpleSocket
builder.AddProject<Demo_SimpleSocket>("demo-simple-socket")
    .WithReference(rabbitmq)
    .WithRabbitMqEnvironment(rabbitmq, rabbitUser, rabbitPassword)
    .WithRedisEnvironment(valkey)
    .WithMySqlEnvironment(mysqlServer, mysqlPassword)
    .WaitForInfrastructure(rabbitmq, valkey, mysql: mysqlDatabase);

// Demo.Web
builder.AddProject<Demo_Web>("demo-web")
    .WithReference(rabbitmq)
    .WithReference(mysqlDatabase)
    .WithRabbitMqEnvironment(rabbitmq, rabbitUser, rabbitPassword)
    .WithRedisEnvironment(valkey)
    .WithMySqlEnvironment(mysqlServer, mysqlPassword)
    .WaitForInfrastructure(rabbitmq, valkey, mysqlDatabase);
    
// GamePulse
builder.AddProject<GamePulse>("game-pulse")
    .WithReference(rabbitmq)
    .WithReference(mysqlDatabase)
    .WithRabbitMqEnvironment(rabbitmq, rabbitUser, rabbitPassword)
    .WithRedisEnvironment(valkey)
    .WithMySqlEnvironment(mysqlServer, mysqlPassword)
    .WaitForInfrastructure(rabbitmq, valkey, mysqlDatabase);

builder.Build().Run();
