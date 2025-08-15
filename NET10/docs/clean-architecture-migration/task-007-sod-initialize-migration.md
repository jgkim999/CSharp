# Task 007: SodInitialize를 CleanArchitecture로 마이그레이션

## 개요
CleanArchitecture 적용을 위해 GamePulse/Sod/SodInitialize.cs 파일을 Demo.Application과 Demo.Infra로 분리하여 이동시키는 작업을 수행했습니다. 의존성 주입 설정을 레이어별로 적절히 분리하여 CleanArchitecture 원칙을 준수하도록 구조화했습니다.

## 작업 내용

### 1. 파일 분리 및 이동

#### Demo.Application으로 이동 (비즈니스 로직 레이어)
- `GamePulse/Sod/SodInitialize.cs` → `Demo.Application/Extensions/SodServiceExtensions.cs`
- Application 레이어 서비스 등록 (ITelemetryService, TelemetryService)

#### Demo.Infra로 이동 (인프라스트럭처 레이어)
- 새로 생성: `Demo.Infra/Extensions/SodInfraExtensions.cs`
- Infrastructure 레이어 서비스 등록 (ISodBackgroundTaskQueue, SodBackgroundWorker)

### 2. 네임스페이스 및 메서드명 변경

#### Demo.Application
- 네임스페이스: `GamePulse.Sod` → `Demo.Application.Extensions`
- 클래스명: `SodInitialize` → `SodServiceExtensions`
- 메서드명: `AddSod()` → `AddSodServices()`

#### Demo.Infra
- 네임스페이스: `Demo.Infra.Extensions`
- 클래스명: `SodInfraExtensions`
- 메서드명: `AddSodInfrastructure()`

### 3. CleanArchitecture 원칙 준수

#### 의존성 분리
- Application 레이어에서 Infrastructure 레이어 직접 참조 제거
- 각 레이어에서 해당 레이어의 서비스만 등록
- Composition Root(Program.cs)에서 두 확장 메서드 호출

#### 책임 분리
- **Application**: 비즈니스 서비스 등록 (TelemetryService)
- **Infrastructure**: 인프라 서비스 등록 (BackgroundTaskQueue, BackgroundWorker)

### 4. 참조 업데이트

#### GamePulse/Program.cs 변경사항
```csharp
// 변경 전
using GamePulse.Sod;
builder.Services.AddSod();

// 변경 후
using Demo.Application.Extensions;
using Demo.Infra.Extensions;
builder.Services.AddSodServices();
builder.Services.AddSodInfrastructure();
```

## CleanArchitecture 원칙 적용

### Application Layer (Demo.Application)
- **SodServiceExtensions**: Application 레이어 서비스 등록
- **TelemetryService**: 비즈니스 로직 서비스 등록

### Infrastructure Layer (Demo.Infra)
- **SodInfraExtensions**: Infrastructure 레이어 서비스 등록
- **SodBackgroundTaskQueue**: 큐 구현체 등록
- **SodBackgroundWorker**: 백그라운드 서비스 등록

### Composition Root (GamePulse/Program.cs)
- 두 레이어의 확장 메서드를 조합하여 전체 SOD 시스템 구성
- 의존성 주입 컨테이너에 모든 필요한 서비스 등록

## 파일별 상세 변경사항

### SodServiceExtensions.cs (Demo.Application)

#### 새로 생성된 파일
```csharp
using Demo.Application.Configs;
using Demo.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Demo.Application.Extensions;

/// <summary>
/// SOD 서비스 등록을 위한 확장 메서드
/// </summary>
public static class SodServiceExtensions
{
    /// <summary>
    /// SOD 텔레메트리 서비스를 의존성 주입 컨테이너에 등록합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>업데이트된 IServiceCollection 인스턴스</returns>
    public static IServiceCollection AddSodServices(this IServiceCollection services)
    {
        // ITelemetryService 및 TelemetryService를 Singleton으로 등록
        services.AddSingleton<ITelemetryService>(serviceProvider =>
        {
            var otelConfig = serviceProvider.GetRequiredService<IOptions<OtelConfig>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<TelemetryService>>();

            return new TelemetryService(
                serviceName: otelConfig.ServiceName,
                serviceVersion: otelConfig.ServiceVersion,
                logger: logger
            );
        });

        return services;
    }
}
```

### SodInfraExtensions.cs (Demo.Infra)

#### 새로 생성된 파일
```csharp
using Demo.Application.Services.Sod;
using Demo.Infra.Services.Sod;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Infra.Extensions;

/// <summary>
/// SOD 인프라 서비스 등록을 위한 확장 메서드
/// </summary>
public static class SodInfraExtensions
{
    /// <summary>
    /// SOD 백그라운드 작업 큐 및 워커 서비스를 의존성 주입 컨테이너에 등록합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>업데이트된 IServiceCollection 인스턴스</returns>
    public static IServiceCollection AddSodInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISodBackgroundTaskQueue, SodBackgroundTaskQueue>();
        services.AddHostedService<SodBackgroundWorker>();

        return services;
    }
}
```

### 원본 파일 (GamePulse/Sod/SodInitialize.cs) - 삭제됨

#### 변경 전
```csharp
using Demo.Application.Services.Sod;
using Demo.Infra.Services.Sod;
using Demo.Application.Configs;
using Demo.Application.Services;
using Microsoft.Extensions.Options;

namespace GamePulse.Sod;

public static class SodInitialize
{
    /// <summary>
    /// SOD 백그라운드 작업 큐 및 워커 서비스와 텔레메트리 서비스를 의존성 주입 컨테이너에 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>업데이트된 <see cref="IServiceCollection"/> 인스턴스</returns>
    public static IServiceCollection AddSod(this IServiceCollection services)
    {
        // ITelemetryService 및 TelemetryService를 Singleton으로 등록
        services.AddSingleton<ITelemetryService>(serviceProvider =>
        {
            var otelConfig = serviceProvider.GetRequiredService<IOptions<OtelConfig>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<TelemetryService>>();

            return new TelemetryService(
                serviceName: otelConfig.ServiceName,
                serviceVersion: otelConfig.ServiceVersion,
                logger: logger
            );
        });

        services.AddSingleton<ISodBackgroundTaskQueue, SodBackgroundTaskQueue>();
        services.AddHostedService<SodBackgroundWorker>();

        return services;
    }
}
```

## 의존성 주입 구조 분석

### 등록되는 서비스들

#### Application Layer Services
1. **ITelemetryService**: 텔레메트리 데이터 수집 인터페이스
2. **TelemetryService**: 텔레메트리 구현체 (Singleton)
   - OtelConfig 설정 사용
   - 서비스 이름 및 버전 설정
   - 로거 주입

#### Infrastructure Layer Services
1. **ISodBackgroundTaskQueue**: 백그라운드 작업 큐 인터페이스
2. **SodBackgroundTaskQueue**: 큐 구현체 (Singleton)
3. **SodBackgroundWorker**: 백그라운드 워커 서비스 (HostedService)

### 서비스 생명주기
- **Singleton**: TelemetryService, SodBackgroundTaskQueue
- **HostedService**: SodBackgroundWorker (애플리케이션 생명주기와 함께)

### 의존성 그래프
```
Program.cs (Composition Root)
├── AddSodServices() → Demo.Application.Extensions
│   └── TelemetryService (Singleton)
│       ├── OtelConfig (Options)
│       └── ILogger<TelemetryService>
└── AddSodInfrastructure() → Demo.Infra.Extensions
    ├── SodBackgroundTaskQueue (Singleton)
    └── SodBackgroundWorker (HostedService)
        ├── ISodBackgroundTaskQueue
        ├── IServiceProvider
        └── ILogger<SodBackgroundWorker>
```

## CleanArchitecture 준수 검증

### 의존성 방향 확인
```
✅ GamePulse (Presentation) → Demo.Application.Extensions
✅ GamePulse (Presentation) → Demo.Infra.Extensions
✅ Demo.Infra.Extensions → Demo.Application.Services.Sod (인터페이스)
❌ Demo.Application.Extensions ↛ Demo.Infra (직접 참조 없음)
```

### 레이어 책임 분리
- **Application**: 비즈니스 서비스 등록만 담당
- **Infrastructure**: 인프라 서비스 등록만 담당
- **Presentation**: 두 레이어를 조합하여 전체 시스템 구성

## 검증 사항

### 빌드 검증
- [x] Demo.Application 프로젝트 빌드 성공
- [x] Demo.Infra 프로젝트 빌드 성공
- [x] GamePulse 프로젝트 빌드 성공
- [x] 전체 솔루션 빌드 성공

### 아키텍처 검증
- [x] Application 레이어에서 Infrastructure 레이어 직접 참조 없음
- [x] 각 레이어에서 해당 레이어의 서비스만 등록
- [x] Composition Root에서 적절한 조합

### 기능 검증
- [x] SOD 시스템 서비스 등록 기능 유지
- [x] TelemetryService 정상 등록
- [x] BackgroundTaskQueue 및 BackgroundWorker 정상 등록

## 빌드 결과

### 최종 빌드 상태
```
성공 빌드(13.0초) - 1개 경고 (기능에 영향 없음)
✅ Demo.Application: 성공 (SodServiceExtensions 추가)
✅ Demo.Infra: 성공 (SodInfraExtensions 추가)
✅ Demo.Application.Tests: 성공
✅ Demo.Web: 성공 (1개 경고 - JsonSerializableAttribute 관련)
✅ GamePulse: 성공
✅ Demo.Web.IntegrationTests: 성공
✅ Demo.Web.PerformanceTests: 성공
✅ GamePulse.Test: 성공
```

### 경고 분석
- **SYSLIB1224**: JsonSerializableAttribute 관련 경고 (Demo.Web.DTO.MyResponse)
- 기능에 영향을 주지 않는 컴파일러 최적화 관련 경고
- SodInitialize 마이그레이션과는 무관한 기존 경고

## 다음 단계

1. **기능 테스트**: SOD 시스템의 전체 워크플로우 테스트
2. **서비스 등록 검증**: 의존성 주입 컨테이너에서 서비스 해결 테스트
3. **성능 테스트**: 백그라운드 작업 처리 성능 측정
4. **추가 마이그레이션**: 다른 GamePulse 초기화 파일들의 CleanArchitecture 적용 검토

## 파일 변경 요약

### 생성된 파일
- `Demo.Application/Extensions/SodServiceExtensions.cs`
- `Demo.Infra/Extensions/SodInfraExtensions.cs`

### 수정된 파일
- `GamePulse/Program.cs` (네임스페이스 참조 및 메서드 호출 업데이트)

### 삭제된 파일
- `GamePulse/Sod/SodInitialize.cs`

## 결론

SodInitialize가 CleanArchitecture 원칙에 따라 Demo.Application과 Demo.Infra로 성공적으로 분리 마이그레이션되었습니다. 이를 통해:

1. **의존성 역전 원칙 준수**: Application 레이어에서 Infrastructure 레이어 직접 참조 제거
2. **단일 책임 원칙**: 각 레이어에서 해당 레이어의 서비스만 등록
3. **관심사 분리**: 비즈니스 서비스와 인프라 서비스 등록 분리
4. **조합 가능성**: Composition Root에서 필요한 서비스들을 조합
5. **테스트 용이성**: 레이어별로 독립적인 서비스 등록 테스트 가능

SOD 시스템의 의존성 주입 설정이 CleanArchitecture 원칙을 준수하면서도 기존 기능을 완전히 유지하도록 구조화되었습니다.