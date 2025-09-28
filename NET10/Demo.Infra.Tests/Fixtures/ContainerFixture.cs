using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit.Abstractions;

namespace Demo.Infra.Tests.Fixtures;

/// <summary>
/// 테스트 간 공유되는 컨테이너들을 관리하는 Fixture
/// RabbitMQ와 Valkey(Redis) 컨테이너를 생성하고 관리합니다
/// </summary>
public class ContainerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private readonly RedisContainer _redisContainer;

    public ContainerFixture()
    {
        // RabbitMQ 테스트 컨테이너 설정
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:4.1.4-management")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .Build();

        // Valkey 테스트 컨테이너 설정 (Redis 호환)
        _redisContainer = new RedisBuilder()
            .WithImage("valkey/valkey:8.1.3-alpine")
            .WithPortBinding(6379, true)
            .Build();
    }

    /// <summary>
    /// RabbitMQ 컨테이너 인스턴스
    /// </summary>
    public RabbitMqContainer RabbitMqContainer => _rabbitMqContainer;

    /// <summary>
    /// Redis(Valkey) 컨테이너 인스턴스
    /// </summary>
    public RedisContainer RedisContainer => _redisContainer;

    /// <summary>
    /// RabbitMQ 연결 문자열
    /// </summary>
    public string RabbitMqConnectionString => _rabbitMqContainer.GetConnectionString();

    /// <summary>
    /// Redis 연결 문자열
    /// </summary>
    public string RedisConnectionString => _redisContainer.GetConnectionString();

    /// <summary>
    /// 테스트 시작 시 컨테이너들을 초기화합니다
    /// </summary>
    public async Task InitializeAsync()
    {
        // 컨테이너들을 병렬로 시작
        await Task.WhenAll(
            _rabbitMqContainer.StartAsync(),
            _redisContainer.StartAsync()
        );
    }

    /// <summary>
    /// 테스트 종료 시 컨테이너들을 정리합니다
    /// </summary>
    public async Task DisposeAsync()
    {
        // 컨테이너들을 병렬로 정리
        await Task.WhenAll(
            _rabbitMqContainer.DisposeAsync().AsTask(),
            _redisContainer.DisposeAsync().AsTask()
        );
    }
}