# Task 3: FusionCache 서비스 등록 및 의존성 주입 설정 구현

## 개요

FusionCache를 애플리케이션에 통합하기 위한 ServiceCollection 확장 메서드를 구현하고, L1(메모리) + L2(Redis) 하이브리드 캐시 구조를 설정했습니다.

## 구현된 기능

### 3.1 ServiceCollection 확장 메서드 구현

#### 파일: `Demo.Infra/Extensions/ServiceCollectionExtensions.cs`

**주요 기능:**
- `AddIpToNationFusionCache` 확장 메서드 생성
- FusionCache 인스턴스를 싱글톤으로 등록
- IDistributedCache로 Redis 구현체 등록
- 설정 클래스들의 의존성 주입 구성

**핵심 구현 사항:**

```csharp
public static IServiceCollection AddIpToNationFusionCache(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // 설정 클래스들 등록
    services.Configure<FusionCacheConfig>(configuration.GetSection("FusionCache"));
    services.Configure<RedisConfig>(configuration.GetSection("Redis"));

    // Redis 연결 및 IDistributedCache 등록
    services.AddSingleton<IConnectionMultiplexer>(...);
    services.AddStackExchangeRedisCache(...);

    // FusionCache 인스턴스 등록
    services.AddSingleton<IFusionCache>(...);

    // IIpToNationCache 인터페이스에 FusionCache 구현체 등록
    services.AddScoped<IIpToNationCache, IpToNationFusionCache>();
}
```

### 3.2 FusionCache 옵션 구성

#### L1 메모리 캐시 설정

**구성된 옵션:**
- `SizeLimit`: 최대 1000개 항목 (설정 가능)
- `CompactionPercentage`: 메모리 압박 시 25% 제거
- `ExpirationScanFrequency`: 1분마다 만료된 항목 스캔

#### L2 Redis 캐시 설정

**구성된 옵션:**
- Redis 연결 문자열 기반 IDistributedCache 설정
- SystemTextJson 직렬화 사용
- 기존 RedisConfig와의 호환성 유지

#### 고급 기능 설정

**페일세이프 메커니즘:**
- `IsFailSafeEnabled`: Redis 장애 시 만료된 캐시 데이터 반환
- `FailSafeMaxDuration`: 최대 1시간까지 만료된 데이터 사용
- `FailSafeThrottleDuration`: 30초 재시도 간격

**백그라운드 새로고침:**
- `EagerRefreshThreshold`: 80% 만료 시점에서 백그라운드 새로고침 시작
- `AllowBackgroundDistributedCacheOperations`: 백그라운드 Redis 작업 허용

**타임아웃 설정:**
- `DistributedCacheSoftTimeout`: 1초 (백그라운드에서 계속 시도)
- `DistributedCacheHardTimeout`: 5초 (완전 중단)

**캐시 스탬피드 방지:**
- `AllowTimedOutFactoryBackgroundCompletion`: 동시 요청 중복 처리 방지
- `SetDurationJittering`: 30초 지터링으로 동시 만료 방지

## 설정 파일 업데이트

### appsettings.json 설정 추가

```json
{
  "Redis": {
    "JwtConnectionString": "192.168.0.47:6379,192.168.0.47:6379",
    "IpToNationConnectionString": "192.168.0.47:6379,192.168.0.47:6379",
    "KeyPrefix": "dev"
  },
  "FusionCache": {
    "DefaultEntryOptions": "00:30:00",
    "L1CacheDuration": "00:05:00",
    "SoftTimeout": "00:00:01",
    "HardTimeout": "00:00:05",
    "EnableFailSafe": true,
    "EnableEagerRefresh": true,
    "FailSafeMaxDuration": "01:00:00",
    "FailSafeThrottleDuration": "00:00:30",
    "EagerRefreshThreshold": 0.8,
    "L1CacheMaxSize": 1000,
    "EnableCacheStampedeProtection": true,
    "EnableOpenTelemetry": true,
    "EnableDetailedLogging": false
  }
}
```

## 이벤트 핸들러 및 로깅

### 등록된 이벤트 핸들러

1. **Hit**: 캐시 히트 시 키, 레벨, 응답 시간 로깅
2. **Miss**: 캐시 미스 시 키 로깅
3. **Set**: 캐시 설정 시 키, 응답 시간 로깅
4. **Remove**: 캐시 제거 시 키 로깅
5. **Expire**: 캐시 만료 시 키 로깅
6. **FailSafeActivate**: 페일세이프 활성화 시 경고 로깅
7. **FactoryError**: 팩토리 오류 시 에러 로깅
8. **BackgroundFactorySuccess**: 백그라운드 새로고침 성공 시 로깅
9. **BackgroundFactoryError**: 백그라운드 새로고침 오류 시 에러 로깅

### OpenTelemetry 계측

- FusionCache 자체 OpenTelemetry 지원 활용
- 커스텀 메트릭 수집을 위한 확장 포인트 제공
- 캐시 히트율, 미스율 등의 성능 지표 수집 준비

## 요구사항 충족 확인

### 요구사항 5.1, 5.3, 6.1 (작업 3.1)
✅ **충족**: 
- IIpToNationCache 인터페이스를 통한 FusionCache 구현체 주입
- appsettings.json을 통한 FusionCache 옵션 구성
- 기존 RedisConfig와의 호환성 유지

### 요구사항 2.1, 2.2, 2.3, 3.1, 3.2, 3.3 (작업 3.2)
✅ **충족**:
- L1 메모리 캐시와 L2 Redis 캐시 계층 구성
- 페일세이프 메커니즘 활성화
- 백그라운드 새로고침 기능 구현
- 캐시 스탬피드 방지 및 타임아웃 설정

## 사용 방법

### 애플리케이션에서 서비스 등록

```csharp
// Program.cs 또는 Startup.cs에서
services.AddIpToNationFusionCache(configuration);
```

### 의존성 주입을 통한 사용

```csharp
public class SomeService
{
    private readonly IIpToNationCache _cache;
    
    public SomeService(IIpToNationCache cache)
    {
        _cache = cache;
    }
    
    public async Task<Result<string>> GetCountryCode(string ip)
    {
        return await _cache.GetAsync(ip);
    }
}
```

## 다음 단계

1. **작업 4**: 기존 Redis 설정과의 호환성 보장
2. **작업 5**: 고급 FusionCache 기능 구현
3. **작업 6**: OpenTelemetry 및 로깅 통합
4. **작업 7**: 단위 테스트 및 통합 테스트 구현

## 참고사항

- FusionCache 패키지가 이미 설치되어 있어야 합니다
- Redis 연결 문자열이 올바르게 설정되어 있어야 합니다
- 기존 IpToNationFusionCache 구현체가 존재해야 합니다
- 설정 파일의 TimeSpan 형식은 "HH:mm:ss" 또는 "d.HH:mm:ss" 형식을 사용합니다