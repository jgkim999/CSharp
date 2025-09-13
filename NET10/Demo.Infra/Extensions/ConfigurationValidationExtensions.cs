using Demo.Infra.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace Demo.Infra.Extensions;

/// <summary>
/// 설정 유효성 검증을 위한 확장 메서드들
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// FusionCache 설정의 유효성을 검증하고 서비스에 등록합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">설정 객체</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddValidatedFusionCacheConfig(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // FusionCache 설정을 바인딩하고 유효성 검증 활성화
        services.Configure<FusionCacheConfig>(configuration.GetSection(FusionCacheConfig.SectionName));
        
        // 데이터 어노테이션 기반 유효성 검증 활성화
        services.AddOptions<FusionCacheConfig>()
            .Bind(configuration.GetSection(FusionCacheConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 커스텀 유효성 검증 추가
        services.AddSingleton<IValidateOptions<FusionCacheConfig>, FusionCacheConfigValidator>();

        // 설정 변경 모니터링 서비스 등록
        services.AddSingleton<IConfigurationChangeMonitor, ConfigurationChangeMonitor>();

        return services;
    }

    /// <summary>
    /// 애플리케이션 시작 시 설정 유효성을 검증합니다
    /// </summary>
    /// <param name="host">호스트 빌더</param>
    /// <returns>호스트 빌더</returns>
    public static IHostBuilder ValidateConfigurationOnStartup(this IHostBuilder host)
    {
        return host.ConfigureServices((context, services) =>
        {
            // 시작 시 설정 검증 서비스 등록
            services.AddHostedService<ConfigurationValidationService>();
        });
    }
}

/// <summary>
/// FusionCache 설정에 대한 커스텀 유효성 검증기
/// </summary>
public class FusionCacheConfigValidator : IValidateOptions<FusionCacheConfig>
{
    private readonly ILogger<FusionCacheConfigValidator> _logger;

    public FusionCacheConfigValidator(ILogger<FusionCacheConfigValidator> logger)
    {
        _logger = logger;
    }

    public ValidateOptionsResult Validate(string? name, FusionCacheConfig options)
    {
        var (isValid, errors) = options.Validate();

        if (!isValid)
        {
            var errorMessage = $"FusionCache 설정 유효성 검증 실패: {string.Join(", ", errors)}";
            _logger.LogError("FusionCache 설정 유효성 검증 실패: {Errors}", string.Join(", ", errors));
            return ValidateOptionsResult.Fail(errors);
        }

        _logger.LogInformation("FusionCache 설정 유효성 검증 성공: {Config}", options.ToString());
        return ValidateOptionsResult.Success;
    }
}

/// <summary>
/// 설정 변경을 모니터링하는 인터페이스
/// </summary>
public interface IConfigurationChangeMonitor
{
    /// <summary>
    /// 설정 변경 이벤트
    /// </summary>
    event Action<FusionCacheConfig> ConfigurationChanged;

    /// <summary>
    /// 현재 설정을 가져옵니다
    /// </summary>
    FusionCacheConfig CurrentConfiguration { get; }
}

/// <summary>
/// 설정 변경을 모니터링하는 서비스
/// </summary>
public class ConfigurationChangeMonitor : IConfigurationChangeMonitor, IDisposable
{
    private readonly IOptionsMonitor<FusionCacheConfig> _optionsMonitor;
    private readonly ILogger<ConfigurationChangeMonitor> _logger;
    private readonly IDisposable _changeListener;

    public event Action<FusionCacheConfig>? ConfigurationChanged;

    public FusionCacheConfig CurrentConfiguration => _optionsMonitor.CurrentValue;

    public ConfigurationChangeMonitor(
        IOptionsMonitor<FusionCacheConfig> optionsMonitor,
        ILogger<ConfigurationChangeMonitor> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;

        // 설정 변경 리스너 등록
        _changeListener = _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(FusionCacheConfig newConfig)
    {
        _logger.LogInformation("FusionCache 설정이 변경되었습니다: {Config}", newConfig.ToString());

        // 새 설정의 유효성 검증
        var (isValid, errors) = newConfig.Validate();
        if (!isValid)
        {
            _logger.LogError("변경된 FusionCache 설정이 유효하지 않습니다: {Errors}", string.Join(", ", errors));
            return;
        }

        // 설정 변경 이벤트 발생
        ConfigurationChanged?.Invoke(newConfig);
    }

    public void Dispose()
    {
        _changeListener?.Dispose();
    }
}

/// <summary>
/// 애플리케이션 시작 시 설정 유효성을 검증하는 호스트 서비스
/// </summary>
public class ConfigurationValidationService : IHostedService
{
    private readonly ILogger<ConfigurationValidationService> _logger;
    private readonly IOptionsMonitor<FusionCacheConfig> _fusionCacheOptions;

    public ConfigurationValidationService(
        ILogger<ConfigurationValidationService> logger,
        IOptionsMonitor<FusionCacheConfig> fusionCacheOptions)
    {
        _logger = logger;
        _fusionCacheOptions = fusionCacheOptions;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // FusionCache 설정 검증
            var fusionCacheConfig = _fusionCacheOptions.CurrentValue;
            var (isValid, errors) = fusionCacheConfig.Validate();

            if (!isValid)
            {
                var errorMessage = $"FusionCache 설정 유효성 검증 실패: {string.Join(", ", errors)}";
                _logger.LogCritical("애플리케이션 시작 실패 - {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation("모든 설정 유효성 검증이 완료되었습니다");
            _logger.LogDebug("FusionCache 설정: {Config}", fusionCacheConfig.ToString());

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "설정 유효성 검증 중 오류가 발생했습니다");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}