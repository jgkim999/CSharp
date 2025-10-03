# Container Collection Fixture 구현 가이드

## 문제점

기존에는 각 테스트 클래스마다 `IClassFixture<ContainerFixture>`를 사용하여 **각 클래스마다 별도의 RabbitMQ와 Valkey 컨테이너**를 생성했습니다.

### 기존 방식의 문제
```csharp
// 각 클래스마다 별도의 컨테이너 생성 ❌
public class RabbitMqPublishServiceTests : IClassFixture<ContainerFixture> { }
public class RabbitMqConnectionTests : IClassFixture<ContainerFixture> { }
public class RedisJwtRepositoryTests : IClassFixture<ContainerFixture> { }
```

**문제:**
- RabbitMQ 컨테이너 3개 생성
- Valkey 컨테이너 3개 생성
- 테스트 실행 시간 증가
- 리소스 낭비

## 해결 방법: Collection Fixture

xUnit의 **Collection Fixture**를 사용하여 **모든 테스트 클래스가 하나의 컨테이너를 공유**하도록 변경했습니다.

### 1. Collection Fixture 정의

`Demo.Infra.Tests/Fixtures/ContainerCollectionFixture.cs`:
```csharp
[CollectionDefinition("Container Collection")]
public class ContainerCollectionFixture : ICollectionFixture<ContainerFixture>
{
    // 마커 클래스 - xUnit이 인식하기 위한 용도
}
```

### 2. 테스트 클래스에 Collection 적용

```csharp
// 모든 테스트가 하나의 컨테이너 공유 ✅
[Collection("Container Collection")]
public class RabbitMqPublishServiceTests { }

[Collection("Container Collection")]
public class RabbitMqConnectionTests { }

[Collection("Container Collection")]
public class RedisJwtRepositoryTests { }

[Collection("Container Collection")]
public class IpToNationRedisCacheTests { }

[Collection("Container Collection")]
public class IpToNationRedisCacheIntegrationTests { }
```

## 적용된 테스트 클래스 (5개)

1. **RabbitMqPublishServiceTests** - RabbitMQ 메시지 발행 테스트
2. **RabbitMqConnectionTests** - RabbitMQ 연결 테스트
3. **RedisJwtRepositoryTests** - Redis JWT 저장소 테스트
4. **IpToNationRedisCacheTests** - Redis 캐시 기본 테스트
5. **IpToNationRedisCacheIntegrationTests** - Redis 캐시 통합 테스트

## 결과

### Before (IClassFixture)
- RabbitMQ 컨테이너: **3개** (각 클래스마다 생성)
- Valkey 컨테이너: **5개** (각 클래스마다 생성)
- 총 컨테이너: **8개**

### After (CollectionFixture)
- RabbitMQ 컨테이너: **1개** (모든 테스트 공유)
- Valkey 컨테이너: **1개** (모든 테스트 공유)
- 총 컨테이너: **2개**

### 성능 개선
- 테스트 실행 시간: **대폭 감소** (컨테이너 시작 시간 절약)
- 리소스 사용: **75% 감소** (8개 → 2개)
- 테스트 병렬성: **향상** (같은 컬렉션 내 테스트는 순차 실행되지만, 다른 컬렉션과는 병렬 실행)

## 주의사항

### 1. 테스트 격리 (Test Isolation)
각 테스트가 **고유한 데이터**를 사용하도록 보장해야 합니다:

```csharp
// ✅ 좋은 예: 고유한 Key Prefix 사용
var testId = Guid.NewGuid().ToString("N")[..8];
var redisConfig = new RedisConfig
{
    IpToNationConnectionString = _containerFixture.RedisConnectionString,
    KeyPrefix = $"test-{testId}"  // 각 테스트마다 고유한 prefix
};

// ✅ 좋은 예: 고유한 Queue/Exchange 이름
var queueName = $"test-queue-{Guid.NewGuid()}";
```

### 2. 순차 실행 (Sequential Execution)
같은 컬렉션 내의 테스트는 **순차적으로 실행**됩니다:
- `RabbitMqPublishServiceTests` → `RabbitMqConnectionTests` → ...
- 빠른 테스트 실행을 위해 **무거운 테스트는 별도 컬렉션으로 분리** 고려

### 3. 컨테이너 라이프사이클
- **생성**: 첫 번째 테스트 실행 전
- **공유**: 모든 테스트가 동일한 인스턴스 사용
- **정리**: 마지막 테스트 완료 후 `DisposeAsync()` 호출

## 추가 개선 가능 사항

### 다른 컬렉션 생성 (필요시)
무거운 통합 테스트를 분리하려면:

```csharp
[CollectionDefinition("Heavy Integration Tests")]
public class HeavyIntegrationTestsCollection : ICollectionFixture<ContainerFixture> { }

[Collection("Heavy Integration Tests")]
public class PerformanceBenchmarkTests { }
```

### 컨테이너 최적화
```csharp
// 더 빠른 시작을 위한 경량 이미지 사용
_rabbitMqContainer = new RabbitMqBuilder()
    .WithImage("rabbitmq:4.1.4-alpine")  // Alpine 이미지 사용
    .Build();
```

## 참고 자료

- [xUnit Collection Fixtures](https://xunit.net/docs/shared-context#collection-fixture)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [Best Practices for Integration Tests](https://learn.microsoft.com/en-us/dotnet/core/testing/integration-tests)

## 결론

Collection Fixture를 사용하면:
- ✅ 컨테이너 생성 횟수 대폭 감소
- ✅ 테스트 실행 시간 단축
- ✅ 리소스 사용 최적화
- ✅ CI/CD 파이프라인 성능 향상

모든 통합 테스트에 이 패턴을 적용하는 것이 권장됩니다.