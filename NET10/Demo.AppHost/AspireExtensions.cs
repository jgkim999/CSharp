using Aspire.Hosting.ApplicationModel;

public static class AspireExtensions
{
    /// <summary>
    /// RabbitMQ 환경 변수를 일괄 설정합니다.
    /// </summary>
    public static IResourceBuilder<T> WithRabbitMqEnvironment<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<RabbitMQServerResource> rabbitmq,
        IResourceBuilder<ParameterResource> rabbitUser,
        IResourceBuilder<ParameterResource> rabbitPassword) where T : IResourceWithEnvironment
    {
        var endpoint = rabbitmq.GetEndpoint("tcp");

        return builder
            .WithEnvironment("RabbitMQ__HostName", endpoint.Property(EndpointProperty.Host))
            .WithEnvironment("RabbitMQ__Port", endpoint.Property(EndpointProperty.Port))
            .WithEnvironment("RabbitMQ__UserName", rabbitUser)
            .WithEnvironment("RabbitMQ__Password", rabbitPassword)
            .WithEnvironment("RabbitMQ__VirtualHost", "/")
            .WithEnvironment("RabbitMQ__UseSsl", "false")
            .WithEnvironment("RabbitMQ__UseTls", "false")
            .WithEnvironment("RabbitMQ__UseTlsCertificateValidation", "false");
    }

    /// <summary>
    /// Redis (Valkey) 환경 변수를 일괄 설정합니다.
    /// </summary>
    public static IResourceBuilder<T> WithRedisEnvironment<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<ContainerResource> valkey) where T : IResourceWithEnvironment
    {
        var endpoint = valkey.GetEndpoint("tcp");

        // Aspire의 connection expression을 사용하여 연결 문자열 구성
        var connectionString = ReferenceExpression.Create(
            $"{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)},allowAdmin=true,abortConnect=false"
        );

        return builder
            .WithEnvironment("Redis__JwtConnectionString", connectionString)
            .WithEnvironment("Redis__IpToNationConnectionString", connectionString)
            .WithEnvironment("Redis__KeyPrefix", "aspire");
    }

    /// <summary>
    /// PostgreSQL 환경 변수를 일괄 설정합니다.
    /// </summary>
    public static IResourceBuilder<T> WithPostgresEnvironment<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<PostgresServerResource> postgresServer,
        IResourceBuilder<ParameterResource> postgresPassword,
        string databaseName = "mydatabase") where T : IResourceWithEnvironment
    {
        var endpoint = postgresServer.GetEndpoint("tcp");

        var connectionString = ReferenceExpression.Create(
            $"Host={endpoint.Property(EndpointProperty.Host)};" +
            $"Port={endpoint.Property(EndpointProperty.Port)};" +
            $"Database={databaseName};" +
            $"Username=postgres;" +
            $"Password={postgresPassword};" +
            $"Maximum Pool Size=8;" +
            $"Minimum Pool Size=2;"
        );

        return builder
            .WithEnvironment("Postgres__ConnectionString", connectionString);
    }

    /// <summary>
    /// MySQL 환경 변수를 일괄 설정합니다.
    /// </summary>
    public static IResourceBuilder<T> WithMySqlEnvironment<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<MySqlServerResource> mysqlServer,
        IResourceBuilder<ParameterResource> mysqlPassword,
        string databaseName = "mydb") where T : IResourceWithEnvironment
    {
        var endpoint = mysqlServer.GetEndpoint("tcp");

        var connectionString = ReferenceExpression.Create(
            $"Server={endpoint.Property(EndpointProperty.Host)};" +
            $"Port={endpoint.Property(EndpointProperty.Port)};" +
            $"Database={databaseName};" +
            $"User=root;" +
            $"Password={mysqlPassword};"
        );

        return builder
            .WithEnvironment("MySQL__ConnectionString", connectionString);
    }

    /// <summary>
    /// 공통 인프라 종속성 대기를 설정합니다.
    /// </summary>
    public static IResourceBuilder<T> WaitForInfrastructure<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<RabbitMQServerResource> rabbitmq,
        IResourceBuilder<ContainerResource> valkey,
        IResourceBuilder<PostgresDatabaseResource>? postgres = null,
        IResourceBuilder<MySqlDatabaseResource>? mysql = null) where T : IResourceWithWaitSupport
    {
        builder = builder
            .WaitFor(rabbitmq)
            .WaitFor(valkey);

        if (postgres is not null)
            builder = builder.WaitFor(postgres);

        if (mysql is not null)
            builder = builder.WaitFor(mysql);

        return builder;
    }
}
