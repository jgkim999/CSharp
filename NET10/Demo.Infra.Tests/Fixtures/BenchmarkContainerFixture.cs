using Testcontainers.Redis;

namespace Demo.Infra.Tests.Fixtures;

/// <summary>
/// 벤치마크 테스트들 간에 공유되는 컨테이너 관리 클래스
/// static 필드를 사용하여 여러 벤치마크 클래스가 동일한 컨테이너를 공유합니다
/// </summary>
public static class BenchmarkContainerFixture
{
    private static RedisContainer? _redisContainer;
    private static readonly SemaphoreSlim _initializationLock = new(1, 1);
    private static int _referenceCount;

    /// <summary>
    /// Redis(Valkey) 컨테이너 인스턴스를 가져옵니다
    /// 처음 호출 시 컨테이너를 생성하고 시작합니다
    /// </summary>
    public static async Task<RedisContainer> GetRedisContainerAsync()
    {
        await _initializationLock.WaitAsync();
        try
        {
            if (_redisContainer == null)
            {
                _redisContainer = new RedisBuilder()
                    .WithImage("valkey/valkey:8.1.3-alpine")
                    .WithPortBinding(6379, true)
                    .Build();

                await _redisContainer.StartAsync();
            }

            _referenceCount++;
            return _redisContainer;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>
    /// Redis 연결 문자열을 가져옵니다
    /// </summary>
    public static string GetRedisConnectionString()
    {
        if (_redisContainer == null)
        {
            throw new InvalidOperationException("Redis container has not been initialized. Call GetRedisContainerAsync first.");
        }

        return _redisContainer.GetConnectionString();
    }

    /// <summary>
    /// 컨테이너 사용을 종료하고 참조 카운트를 감소시킵니다
    /// 모든 참조가 해제되면 컨테이너를 정리합니다
    /// </summary>
    public static async Task ReleaseAsync()
    {
        await _initializationLock.WaitAsync();
        try
        {
            _referenceCount--;

            if (_referenceCount <= 0 && _redisContainer != null)
            {
                await _redisContainer.DisposeAsync();
                _redisContainer = null;
                _referenceCount = 0;
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>
    /// 강제로 컨테이너를 정리합니다 (테스트 종료 시)
    /// </summary>
    public static async Task ForceCleanupAsync()
    {
        await _initializationLock.WaitAsync();
        try
        {
            if (_redisContainer != null)
            {
                await _redisContainer.DisposeAsync();
                _redisContainer = null;
                _referenceCount = 0;
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}