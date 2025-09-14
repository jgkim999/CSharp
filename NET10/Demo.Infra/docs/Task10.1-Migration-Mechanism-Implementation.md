# Task 10.1 - 기존 구현체와의 전환 메커니즘 구현

## 개요

FusionCache로의 점진적 마이그레이션을 지원하기 위한 전환 메커니즘을 구현했습니다. 이 메커니즘은 기능 플래그와 트래픽 분할을 통해 안전한 전환을 가능하게 합니다.

## 구현된 컴포넌트

### 1. FusionCacheConfig 확장

**파일**: `Demo.Infra/Configs/FusionCacheConfig.cs`

전환 메커니즘을 위한 새로운 설정 속성들을 추가했습니다:

```csharp
/// <summary>
/// FusionCache 사용 여부를 결정하는 기능 플래그
/// </summary>
public bool UseFusionCache { get; set; } = false;

/// <summary>
/// 점진적 전환을 위한 트래픽 비율 (0.0 ~ 1.0)
/// </summary>
public double TrafficSplitRatio { get; set; } = 0.0;

/// <summary>
/// 트래픽 분할 시 사용할 해시 시드
/// </summary>
public int TrafficSplitHashSeed { get; set; } = 12345;
```

### 2. IpToNationCacheWrapper 클래스

**파일**: `Demo.Infra/Repositories/IpToNationCacheWrapper.cs`

두 구현체 간의 전환을 관리하는 래퍼 클래스입니다:

#### 주요 기능

- **기능 플래그 기반 전환**: `UseFusionCache` 설정에 따른 구현체 선택
- **트래픽 분할**: IP 주소 기반 일관된 해시를 통한 점진적 전환
- **폴백 메커니즘**: FusionCache 오류 시 기존 Redis 캐시로 자동 폴백
- **보안**: IP 주소 로깅 시 해시 처리로 개인정보 보호

#### 트래픽 분할 알고리즘

```csharp
private bool ShouldUseNewImplementation(string clientIp, FusionCacheConfig config)
{
    // 기능 플래그 확인
    if (!config.UseFusionCache) return false;
    if (config.TrafficSplitRatio >= 1.0) return true;
    if (config.TrafficSplitRatio <= 0.0) return false;

    // IP 기반 일관된 해시 생성
    var hash = ComputeConsistentHash(clientIp, config.TrafficSplitHashSeed);
    var normalizedHash = hash / (double)uint.MaxValue;

    return normalizedHash < config.TrafficSplitRatio;
}
```

### 3. ServiceCollection 확장 메서드

**파일**: `Demo.Infra/Extensions/ServiceCollectionExtensions.cs`

전환 메커니즘을 지원하는 새로운 확장 메서드들을 추가했습니다:

#### AddIpToNationCacheWithMigration

```csharp
public static IServiceCollection AddIpToNationCacheWithMigration(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // 기존 Redis 캐시와 FusionCache 모두 등록
    services.AddIpToNationRedisCache(configuration);
    services.AddIpToNationFusionCache(configuration);
    
    // 래퍼 등록 및 동적 구현체 선택
    services.AddScoped<IpToNationCacheWrapper>();
    services.AddScoped<IIpToNationCache>(serviceProvider => {
        var config = serviceProvider.GetRequiredService<IOptionsMonitor<FusionCacheConfig>>().CurrentValue;
        
        if (config.UseFusionCache || config.TrafficSplitRatio > 0.0)
        {
            return serviceProvider.GetRequiredService<IpToNationCacheWrapper>();
        }
        
        return serviceProvider.GetRequiredService<IpToNationRedisCache>();
    });
    
    return services;
}
```

### 4. 설정 파일 업데이트

#### appsettings.json (프로덕션)

```json
{
  "FusionCache": {
    "UseFusionCache": false,
    "TrafficSplitRatio": 0.0,
    "TrafficSplitHashSeed": 12345
  }
}
```

#### appsettings.Development.json (개발)

```json
{
  "FusionCache": {
    "UseFusionCache": true,
    "TrafficSplitRatio": 0.1,
    "TrafficSplitHashSeed": 12345
  }
}
```

### 5. 단위 테스트

**파일**: `Demo.Infra.Tests/Repositories/IpToNationCacheWrapperTests.cs`

전환 메커니즘의 정확성을 검증하는 포괄적인 테스트 스위트:

- 기능 플래그 기반 라우팅 테스트
- 트래픽 분할 비율 검증
- 동일 IP에 대한 일관된 라우팅 확인
- 폴백 메커니즘 테스트
- 설정 유효성 검증

## 사용 방법

### 1. 기본 설정 (기존 Redis 캐시만 사용)

```json
{
  "FusionCache": {
    "UseFusionCache": false,
    "TrafficSplitRatio": 0.0
  }
}
```

### 2. 점진적 전환 (10% 트래픽을 FusionCache로)

```json
{
  "FusionCache": {
    "UseFusionCache": true,
    "TrafficSplitRatio": 0.1
  }
}
```

### 3. 완전 전환 (모든 트래픽을 FusionCache로)

```json
{
  "FusionCache": {
    "UseFusionCache": true,
    "TrafficSplitRatio": 1.0
  }
}
```

### 4. 서비스 등록

```csharp
// Program.cs 또는 Startup.cs에서
services.AddIpToNationCacheWithMigration(configuration);
```

## 모니터링 및 로깅

### 로그 메시지 예시

```
[DEBUG] FusionCache를 사용하여 IP ***12AB에 대한 국가 코드를 조회합니다
[DEBUG] 기존 Redis 캐시를 사용하여 IP ***34CD에 대한 국가 코드를 조회합니다
[WARNING] FusionCache 오류로 인해 기존 Redis 캐시로 폴백합니다
```

### 메트릭

- 구현체별 요청 수
- 폴백 발생 횟수
- 오류율 비교

## 보안 고려사항

1. **IP 주소 보호**: 로깅 시 IP 주소를 SHA256 해시의 첫 8자리로 마스킹
2. **일관된 라우팅**: 동일한 IP에 대해 항상 동일한 구현체 사용
3. **설정 검증**: 잘못된 설정 값에 대한 유효성 검사

## 성능 영향

- **해시 계산 오버헤드**: IP당 약 0.1ms 미만의 추가 지연
- **메모리 사용량**: 래퍼 클래스로 인한 미미한 증가
- **CPU 사용량**: SHA256 해시 계산으로 인한 경미한 증가

## 롤백 계획

1. **즉시 롤백**: `UseFusionCache: false` 설정으로 즉시 기존 구현체로 복귀
2. **점진적 롤백**: `TrafficSplitRatio`를 점진적으로 감소시켜 안전한 롤백
3. **완전 제거**: 래퍼 제거 후 기존 서비스 등록 방식으로 복귀

## 다음 단계

1. 개발 환경에서 10% 트래픽으로 테스트
2. 스테이징 환경에서 50% 트래픽으로 검증
3. 프로덕션 환경에서 점진적 전환 (5% → 25% → 50% → 100%)
4. 모니터링 결과에 따른 최적화