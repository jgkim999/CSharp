# 설계 문서

## 개요

IpToNationRedisCache를 FusionCache로 마이그레이션하여 L1(메모리) + L2(Redis) 하이브리드 캐시 구조를 구현합니다. FusionCache는 캐시 스탬피드 방지, 페일세이프 메커니즘, 백그라운드 새로고침 등의 고급 기능을 제공하여 기존 구현보다 더 나은 성능과 안정성을 제공합니다.

## 아키텍처

### 현재 아키텍처

```
Application Layer
       ↓
IIpToNationCache (인터페이스)
       ↓
IpToNationRedisCache (구현체)
       ↓
StackExchange.Redis → Redis Server
```

### 새로운 아키텍처

```
Application Layer
       ↓
IIpToNationCache (인터페이스)
       ↓
IpToNationFusionCache (새 구현체)
       ↓
FusionCache
    ↓        ↓
L1 Cache   L2 Cache (IDistributedCache)
(Memory)        ↓
            Redis Implementation
                ↓
            Redis Server
```

### 계층별 역할

1. **L1 Cache (메모리)**
   - 가장 빠른 액세스를 위한 인메모리 캐시
   - 자주 액세스되는 IP-국가 매핑 저장
   - 기본 TTL: 5분

2. **L2 Cache (Redis)**
   - 분산 캐시로 여러 인스턴스 간 데이터 공유
   - L1 캐시 미스 시 백업 데이터 소스
   - 기존 Redis 설정 및 데이터와 호환

3. **FusionCache 레이어**
   - L1과 L2 간의 투명한 조정
   - 고급 기능 제공 (스탬피드 방지, 페일세이프 등)

## 컴포넌트 및 인터페이스

### 1. IpToNationFusionCache 클래스

```csharp
public class IpToNationFusionCache : IIpToNationCache
{
    private readonly IFusionCache _fusionCache;
    private readonly string? _keyPrefix;
    private readonly ILogger<IpToNationFusionCache> _logger;
    
    // 생성자, GetAsync, SetAsync 메서드 구현
}
```

**주요 책임:**

- IIpToNationCache 인터페이스 구현
- FusionCache를 통한 캐시 작업 수행
- 키 생성 및 관리
- 오류 처리 및 로깅

### 2. FusionCache 설정 클래스

```csharp
public class FusionCacheConfig
{
    public TimeSpan DefaultEntryOptions { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan L1CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SoftTimeout { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan HardTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public bool EnableFailSafe { get; set; } = true;
    public bool EnableEagerRefresh { get; set; } = true;
}
```

### 3. 의존성 주입 확장

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIpToNationFusionCache(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // FusionCache 및 관련 서비스 등록
    }
}
```

## 데이터 모델

### 캐시 키 구조

기존과 동일한 키 형식을 유지합니다:

- 접두사가 있는 경우: `{prefix}:ipcache:{clientIp}`
- 접두사가 없는 경우: `ipcache:{clientIp}`

### 캐시 값 구조

- **타입**: `string` (국가 코드)
- **직렬화**: 문자열이므로 추가 직렬화 불필요
- **예시**: `"KR"`, `"US"`, `"JP"`

### 캐시 옵션 구조

```csharp
var entryOptions = new FusionCacheEntryOptions
{
    Duration = duration,
    Priority = CacheItemPriority.Normal,
    Size = 1,
    FailSafeMaxDuration = TimeSpan.FromHours(1),
    FailSafeThrottleDuration = TimeSpan.FromSeconds(30)
};
```

## 오류 처리

### 1. Redis 연결 실패

- **기존**: Polly 재시도 정책 사용
- **새로운 방식**: FusionCache의 내장 페일세이프 메커니즘 활용
- **동작**: L2 실패 시 L1 캐시만으로 계속 작동

### 2. 캐시 미스 처리

- **기존**: `Result.Fail("Not found")` 반환
- **새로운 방식**: 동일한 결과 반환, 내부적으로 FusionCache 최적화 적용

### 3. 타임아웃 처리

- **Soft Timeout**: 1초 (백그라운드에서 계속 시도)
- **Hard Timeout**: 5초 (완전 중단)
- **페일세이프**: 만료된 캐시 데이터라도 반환

### 4. 동시성 제어

- **캐시 스탬피드 방지**: 동일 키에 대한 동시 요청 시 하나만 실행
- **백그라운드 새로고침**: 만료 전 자동 갱신

## 테스트 전략

### 1. 단위 테스트

- **기존 테스트 유지**: IIpToNationCache 인터페이스 계약 검증
- **새로운 테스트**: FusionCache 특화 기능 테스트
- **모킹**: IFusionCache 인터페이스 모킹으로 격리된 테스트

### 2. 통합 테스트

- **Redis 통합**: 실제 Redis 인스턴스와의 통합 테스트
- **L1/L2 계층 테스트**: 메모리와 Redis 간의 데이터 동기화 검증
- **페일오버 테스트**: Redis 장애 시나리오 테스트

### 3. 성능 테스트

- **응답 시간**: L1 vs L2 캐시 히트 시간 측정
- **처리량**: 동시 요청 처리 능력 측정
- **메모리 사용량**: L1 캐시 메모리 사용량 모니터링

### 4. 회귀 테스트

- **기존 기능**: 모든 기존 테스트가 통과해야 함
- **호환성**: 기존 Redis 데이터와의 호환성 검증
- **설정**: 기존 RedisConfig 설정 호환성 확인

## 마이그레이션 계획

### 1. 단계별 접근

1. **Phase 1**: FusionCache 패키지 추가 및 기본 설정
2. **Phase 2**: IpToNationFusionCache 구현체 개발
3. **Phase 3**: 의존성 주입 설정 업데이트
4. **Phase 4**: 테스트 및 검증
5. **Phase 5**: 기존 구현체 교체

### 2. 호환성 보장

- **인터페이스**: IIpToNationCache 인터페이스 변경 없음
- **키 형식**: 기존 Redis 키 형식 유지
- **설정**: 기존 RedisConfig 재사용
- **데이터**: 기존 Redis 데이터 그대로 사용 가능

### 3. 롤백 계획

- **설정 스위치**: appsettings.json에서 구현체 선택 가능
- **기존 코드 보존**: IpToNationRedisCache 클래스 유지
- **점진적 전환**: 기능 플래그를 통한 단계적 전환

## 모니터링 및 관찰성

### 1. OpenTelemetry 통합

- **메트릭**: 캐시 히트율, 미스율, 응답 시간
- **추적**: FusionCache 작업의 분산 추적
- **로그**: 구조화된 로그 출력

### 2. 성능 지표

- **L1 히트율**: 메모리 캐시 효율성
- **L2 히트율**: Redis 캐시 효율성
- **평균 응답 시간**: 캐시 작업 성능
- **오류율**: 캐시 작업 실패율

### 3. 알림 및 경고

- **Redis 연결 실패**: L2 캐시 장애 알림
- **높은 미스율**: 캐시 효율성 저하 경고
- **메모리 사용량**: L1 캐시 메모리 사용량 모니터링

## 보안 고려사항

### 1. 데이터 보호

- **전송 중 암호화**: Redis 연결 시 TLS 사용
- **저장 시 암호화**: Redis 서버 레벨 암호화
- **액세스 제어**: Redis 인증 및 권한 관리

### 2. 캐시 보안

- **키 네임스페이싱**: 접두사를 통한 데이터 격리
- **TTL 관리**: 적절한 만료 시간 설정으로 데이터 노출 최소화
- **로그 보안**: 민감한 IP 정보 로깅 시 마스킹

## 성능 최적화

### 1. 메모리 관리

- **L1 캐시 크기 제한**: 메모리 사용량 제어
- **LRU 정책**: 가장 적게 사용된 항목 자동 제거
- **압축**: 필요시 캐시 값 압축

### 2. 네트워크 최적화

- **연결 풀링**: Redis 연결 재사용
- **배치 작업**: 가능한 경우 배치 처리
- **압축**: Redis 통신 시 압축 활용

### 3. 캐시 전략

- **적응형 TTL**: 사용 패턴에 따른 동적 TTL 조정
- **백그라운드 새로고침**: 사용자 요청 전 미리 갱신
- **지능형 프리로딩**: 예측 가능한 데이터 미리 로드
