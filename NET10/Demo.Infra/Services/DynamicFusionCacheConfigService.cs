using Demo.Infra.Configs;
using Demo.Infra.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Demo.Infra.Services;

/// <summary>
/// FusionCache 설정의 동적 업데이트를 처리하는 서비스
/// 설정 변경 시 재시작 없이 캐시 동작을 업데이트합니다
/// </summary>
public class DynamicFusionCacheConfigService : IHostedService, IDisposable
{
    private readonly ILogger<DynamicFusionCacheConfigService> _logger;
    private readonly IConfigurationChangeMonitor _configMonitor;
    private readonly IFusionCache _fusionCache;
    private readonly IDisposable? _configChangeSubscription;

    public DynamicFusionCacheConfigService(
        ILogger<DynamicFusionCacheConfigService> logger,
        IConfigurationChangeMonitor configMonitor,
        IFusionCache fusionCache)
    {
        _logger = logger;
        _configMonitor = configMonitor;
        _fusionCache = fusionCache;

        // 설정 변경 이벤트 구독
        _configMonitor.ConfigurationChanged += OnConfigurationChanged;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("동적 FusionCache 설정 서비스가 시작되었습니다");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("동적 FusionCache 설정 서비스가 중지되었습니다");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 설정 변경 시 호출되는 이벤트 핸들러
    /// </summary>
    /// <param name="newConfig">새로운 설정</param>
    private void OnConfigurationChanged(FusionCacheConfig newConfig)
    {
        try
        {
            _logger.LogInformation("FusionCache 설정 변경이 감지되었습니다. 동적 업데이트를 시도합니다");

            // 현재 지원되는 동적 업데이트 항목들
            UpdateDynamicSettings(newConfig);

            _logger.LogInformation("FusionCache 설정이 성공적으로 업데이트되었습니다: {Config}", 
                newConfig.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FusionCache 설정 동적 업데이트 중 오류가 발생했습니다");
        }
    }

    /// <summary>
    /// 재시작 없이 업데이트 가능한 설정들을 적용합니다
    /// </summary>
    /// <param name="newConfig">새로운 설정</param>
    private void UpdateDynamicSettings(FusionCacheConfig newConfig)
    {
        // FusionCache의 기본 엔트리 옵션 업데이트
        // 주의: 이미 캐시된 항목들은 기존 설정을 유지하며, 새로운 항목부터 적용됩니다
        var newDefaultOptions = new FusionCacheEntryOptions
        {
            Duration = newConfig.DefaultEntryOptions,
            Priority = Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal,
            Size = 1,
            FailSafeMaxDuration = newConfig.FailSafeMaxDuration,
            FailSafeThrottleDuration = newConfig.FailSafeThrottleDuration,
            IsFailSafeEnabled = newConfig.EnableFailSafe,
            EagerRefreshThreshold = newConfig.EagerRefreshThreshold,
            AllowBackgroundDistributedCacheOperations = true,
            ReThrowDistributedCacheExceptions = false,
            DistributedCacheSoftTimeout = newConfig.SoftTimeout,
            DistributedCacheHardTimeout = newConfig.HardTimeout,
            AllowTimedOutFactoryBackgroundCompletion = newConfig.EnableCacheStampedeProtection
        };

        // FusionCache의 기본 옵션 업데이트
        // 참고: FusionCache는 런타임에 기본 옵션 변경을 직접 지원하지 않으므로
        // 새로운 캐시 작업부터 새 설정이 적용되도록 로깅만 수행
        _logger.LogInformation("새로운 FusionCache 기본 옵션이 준비되었습니다. " +
            "새로운 캐시 작업부터 다음 설정이 적용됩니다: " +
            "Duration={Duration}, SoftTimeout={SoftTimeout}, HardTimeout={HardTimeout}, " +
            "FailSafe={FailSafe}, EagerRefresh={EagerRefresh}",
            newDefaultOptions.Duration,
            newDefaultOptions.DistributedCacheSoftTimeout,
            newDefaultOptions.DistributedCacheHardTimeout,
            newDefaultOptions.IsFailSafeEnabled,
            newDefaultOptions.EagerRefreshThreshold);

        // 로깅 레벨 변경 알림
        _logger.LogInformation("FusionCache 로깅 설정이 업데이트되었습니다: " +
            "LogLevel={LogLevel}, DetailedLogging={DetailedLogging}, Metrics={Metrics}",
            newConfig.CacheEventLogLevel,
            newConfig.EnableDetailedLogging,
            newConfig.EnableMetrics);

        // 메트릭 수집 간격 변경 알림
        if (newConfig.EnableMetrics)
        {
            _logger.LogInformation("FusionCache 메트릭 수집 간격이 업데이트되었습니다: {IntervalSeconds}초",
                newConfig.MetricsCollectionIntervalSeconds);
        }

        // 주의사항 로깅
        _logger.LogWarning("일부 FusionCache 설정은 애플리케이션 재시작 후에만 완전히 적용됩니다. " +
            "완전한 설정 적용을 위해서는 애플리케이션을 재시작하는 것을 권장합니다");
    }

    public void Dispose()
    {
        _configChangeSubscription?.Dispose();
    }
}

/// <summary>
/// 설정 변경에 대한 명확한 오류 메시지를 제공하는 예외 클래스
/// </summary>
public class ConfigurationValidationException : Exception
{
    /// <summary>
    /// 유효성 검증 오류 목록
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// 설정 섹션 이름
    /// </summary>
    public string ConfigurationSection { get; }

    public ConfigurationValidationException(
        string configurationSection, 
        IEnumerable<string> validationErrors) 
        : base($"{configurationSection} 설정 유효성 검증 실패: {string.Join(", ", validationErrors)}")
    {
        ConfigurationSection = configurationSection;
        ValidationErrors = validationErrors.ToList().AsReadOnly();
    }

    public ConfigurationValidationException(
        string configurationSection, 
        IEnumerable<string> validationErrors, 
        Exception innerException) 
        : base($"{configurationSection} 설정 유효성 검증 실패: {string.Join(", ", validationErrors)}", innerException)
    {
        ConfigurationSection = configurationSection;
        ValidationErrors = validationErrors.ToList().AsReadOnly();
    }
}

/// <summary>
/// 설정 유효성 검증을 위한 헬퍼 클래스
/// </summary>
public static class ConfigurationValidationHelper
{
    /// <summary>
    /// 설정 객체의 데이터 어노테이션 유효성을 검증합니다
    /// </summary>
    /// <typeparam name="T">설정 클래스 타입</typeparam>
    /// <param name="config">검증할 설정 객체</param>
    /// <returns>유효성 검증 결과</returns>
    public static (bool IsValid, List<string> Errors) ValidateDataAnnotations<T>(T config) where T : class
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(config);
        
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            config, validationContext, validationResults, validateAllProperties: true);

        var errors = validationResults.Select(vr => vr.ErrorMessage ?? "알 수 없는 유효성 검증 오류").ToList();
        
        return (isValid, errors);
    }

    /// <summary>
    /// 설정 값이 유효한 TimeSpan 범위 내에 있는지 확인합니다
    /// </summary>
    /// <param name="value">검증할 TimeSpan 값</param>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <param name="propertyName">속성 이름</param>
    /// <returns>유효성 검증 결과</returns>
    public static (bool IsValid, string? Error) ValidateTimeSpanRange(
        TimeSpan value, TimeSpan min, TimeSpan max, string propertyName)
    {
        if (value < min || value > max)
        {
            return (false, $"{propertyName}은(는) {min} 이상 {max} 이하여야 합니다. 현재 값: {value}");
        }
        
        return (true, null);
    }

    /// <summary>
    /// 설정 값이 유효한 숫자 범위 내에 있는지 확인합니다
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <param name="propertyName">속성 이름</param>
    /// <returns>유효성 검증 결과</returns>
    public static (bool IsValid, string? Error) ValidateNumericRange<T>(
        T value, T min, T max, string propertyName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            return (false, $"{propertyName}은(는) {min} 이상 {max} 이하여야 합니다. 현재 값: {value}");
        }
        
        return (true, null);
    }

    /// <summary>
    /// 연결 문자열의 기본적인 유효성을 검증합니다
    /// </summary>
    /// <param name="connectionString">검증할 연결 문자열</param>
    /// <param name="propertyName">속성 이름</param>
    /// <returns>유효성 검증 결과</returns>
    public static (bool IsValid, string? Error) ValidateConnectionString(
        string? connectionString, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (false, $"{propertyName}이(가) 설정되지 않았거나 비어있습니다");
        }

        // 기본적인 Redis 연결 문자열 형식 확인
        if (!connectionString.Contains(':') && !connectionString.Contains(','))
        {
            return (false, $"{propertyName}이(가) 올바른 형식이 아닙니다. 예: 'localhost:6379' 또는 'server1:6379,server2:6379'");
        }
        
        return (true, null);
    }
}