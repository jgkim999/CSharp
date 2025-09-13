using Demo.Domain.Repositories;
using Demo.Infra.Configs;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Demo.Infra.Repositories;

/// <summary>
/// IpToNation 캐시 구현체 간의 전환을 관리하는 래퍼 클래스
/// 기능 플래그와 트래픽 분할을 통한 점진적 마이그레이션을 지원합니다
/// </summary>
public class IpToNationCacheWrapper : IIpToNationCache
{
  private readonly IIpToNationCache _fusionCache;
  private readonly IIpToNationCache _redisCache;
  private readonly IOptionsMonitor<FusionCacheConfig> _configMonitor;
  private readonly ILogger<IpToNationCacheWrapper> _logger;

  /// <summary>
  /// IpToNationCacheWrapper 생성자
  /// </summary>
  /// <param name="fusionCache">FusionCache 구현체</param>
  /// <param name="redisCache">기존 Redis 캐시 구현체</param>
  /// <param name="configMonitor">FusionCache 설정 모니터</param>
  /// <param name="logger">로거</param>
  public IpToNationCacheWrapper(
      IIpToNationCache fusionCache,
      IIpToNationCache redisCache,
      IOptionsMonitor<FusionCacheConfig> configMonitor,
      ILogger<IpToNationCacheWrapper> logger)
  {
    _fusionCache = fusionCache ?? throw new ArgumentNullException(nameof(fusionCache));
    _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
    _configMonitor = configMonitor ?? throw new ArgumentNullException(nameof(configMonitor));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <summary>
  /// 캐시에서 IP 주소에 대한 국가 코드를 비동기적으로 조회합니다
  /// 설정에 따라 FusionCache 또는 기존 Redis 캐시를 사용합니다
  /// </summary>
  /// <param name="clientIp">조회할 클라이언트 IP 주소</param>
  /// <returns>국가 코드 또는 실패 결과</returns>
  public async Task<Result<string>> GetAsync(string clientIp)
  {
    var config = _configMonitor.CurrentValue;
    var useNewImplementation = ShouldUseNewImplementation(clientIp, config);

    try
    {
      if (useNewImplementation)
      {
        _logger.LogDebug("FusionCache를 사용하여 IP {ClientIp}에 대한 국가 코드를 조회합니다",
            HashIpForLogging(clientIp));
        return await _fusionCache.GetAsync(clientIp);
      }
      else
      {
        _logger.LogDebug("기존 Redis 캐시를 사용하여 IP {ClientIp}에 대한 국가 코드를 조회합니다",
            HashIpForLogging(clientIp));
        return await _redisCache.GetAsync(clientIp);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "IP {ClientIp}에 대한 캐시 조회 중 오류가 발생했습니다. 구현체: {Implementation}",
          HashIpForLogging(clientIp), useNewImplementation ? "FusionCache" : "RedisCache");

      // 오류 발생 시 폴백 시도
      if (useNewImplementation && config.TrafficSplitRatio < 1.0)
      {
        _logger.LogWarning("FusionCache 오류로 인해 기존 Redis 캐시로 폴백합니다");
        return await _redisCache.GetAsync(clientIp);
      }

      throw;
    }
  }

  /// <summary>
  /// 캐시에 IP 주소와 국가 코드 매핑을 비동기적으로 저장합니다
  /// 설정에 따라 FusionCache 또는 기존 Redis 캐시를 사용합니다
  /// </summary>
  /// <param name="clientIp">저장할 클라이언트 IP 주소</param>
  /// <param name="countryCode">저장할 국가 코드</param>
  /// <param name="expiration">캐시 만료 시간</param>
  /// <returns>저장 작업</returns>
  public async Task SetAsync(string clientIp, string countryCode, TimeSpan expiration)
  {
    var config = _configMonitor.CurrentValue;
    var useNewImplementation = ShouldUseNewImplementation(clientIp, config);

    try
    {
      if (useNewImplementation)
      {
        _logger.LogDebug("FusionCache를 사용하여 IP {ClientIp}에 대한 국가 코드 {CountryCode}를 저장합니다",
            HashIpForLogging(clientIp), countryCode);
        await _fusionCache.SetAsync(clientIp, countryCode, expiration);
      }
      else
      {
        _logger.LogDebug("기존 Redis 캐시를 사용하여 IP {ClientIp}에 대한 국가 코드 {CountryCode}를 저장합니다",
            HashIpForLogging(clientIp), countryCode);
        await _redisCache.SetAsync(clientIp, countryCode, expiration);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "IP {ClientIp}에 대한 캐시 저장 중 오류가 발생했습니다. 구현체: {Implementation}",
          HashIpForLogging(clientIp), useNewImplementation ? "FusionCache" : "RedisCache");

      // 오류 발생 시 폴백 시도
      if (useNewImplementation && config.TrafficSplitRatio < 1.0)
      {
        _logger.LogWarning("FusionCache 오류로 인해 기존 Redis 캐시로 폴백합니다");
        await _redisCache.SetAsync(clientIp, countryCode, expiration);
        return;
      }

      throw;
    }
  }

  /// <summary>
  /// 주어진 IP 주소와 설정에 따라 새로운 구현체(FusionCache)를 사용할지 결정합니다
  /// </summary>
  /// <param name="clientIp">클라이언트 IP 주소</param>
  /// <param name="config">FusionCache 설정</param>
  /// <returns>새로운 구현체 사용 여부</returns>
  private bool ShouldUseNewImplementation(string clientIp, FusionCacheConfig config)
  {
    // 기능 플래그가 비활성화된 경우 기존 구현체 사용
    if (!config.UseFusionCache)
    {
      return false;
    }

    // 트래픽 분할 비율이 1.0인 경우 모든 트래픽이 새로운 구현체 사용
    if (config.TrafficSplitRatio >= 1.0)
    {
      return true;
    }

    // 트래픽 분할 비율이 0.0인 경우 모든 트래픽이 기존 구현체 사용
    if (config.TrafficSplitRatio <= 0.0)
    {
      return false;
    }

    // IP 주소를 기반으로 일관된 해시 생성
    var hash = ComputeConsistentHash(clientIp, config.TrafficSplitHashSeed);
    var normalizedHash = hash / (double)uint.MaxValue;

    // 해시 값이 트래픽 분할 비율보다 작으면 새로운 구현체 사용
    return normalizedHash < config.TrafficSplitRatio;
  }

  /// <summary>
  /// IP 주소와 시드를 사용하여 일관된 해시를 계산합니다
  /// 동일한 IP에 대해 항상 동일한 해시 값을 반환하여 일관된 라우팅을 보장합니다
  /// </summary>
  /// <param name="clientIp">클라이언트 IP 주소</param>
  /// <param name="seed">해시 시드</param>
  /// <returns>해시 값</returns>
  private static uint ComputeConsistentHash(string clientIp, int seed)
  {
    var input = $"{clientIp}:{seed}";
    var inputBytes = Encoding.UTF8.GetBytes(input);

    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(inputBytes);

    // 해시의 첫 4바이트를 uint로 변환
    return BitConverter.ToUInt32(hashBytes, 0);
  }

  /// <summary>
  /// 로깅을 위해 IP 주소를 해시합니다 (개인정보 보호)
  /// </summary>
  /// <param name="clientIp">클라이언트 IP 주소</param>
  /// <returns>해시된 IP 주소</returns>
  private static string HashIpForLogging(string clientIp)
  {
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(clientIp));
    return Convert.ToHexString(hashBytes)[..8]; // 처음 8자리만 사용
  }
}