# Task 1: FusionCache 패키지 설치 및 기본 설정 구성 구현

## 개요

이 문서는 FusionCache 통합의 첫 번째 단계로, 필요한 NuGet 패키지 설치와 기본 설정 클래스 구성에 대한 구현 내용을 설명합니다.

## 구현된 작업

### 1. NuGet 패키지 설치

Demo.Infra 프로젝트에 다음 패키지들을 추가했습니다:

- **ZiggyCreatures.FusionCache (v2.4.0)**: 핵심 FusionCache 라이브러리
- **ZiggyCreatures.FusionCache.Serialization.SystemTextJson (v2.4.0)**: System.Text.Json 직렬화 지원

### 2. FusionCacheConfig 설정 클래스 생성

`Demo.Infra/Configs/FusionCacheConfig.cs` 파일을 생성하여 FusionCache의 모든 주요 설정 옵션을 정의했습니다.

#### 주요 설정 항목

- **캐시 지속 시간 설정**
  - `DefaultEntryOptions`: 기본 캐시 항목 지속 시간 (30분)
  - `L1CacheDuration`: L1 메모리 캐시 지속 시간 (5분)

- **타임아웃 설정**
  - `SoftTimeout`: 소프트 타임아웃 (1초)
  - `HardTimeout`: 하드 타임아웃 (5초)

- **고급 기능 설정**
  - `EnableFailSafe`: 페일세이프 메커니즘 활성화
  - `EnableEagerRefresh`: 백그라운드 새로고침 활성화
  - `EnableCacheStampedeProtection`: 캐시 스탬피드 방지 활성화

- **성능 및 모니터링 설정**
  - `L1CacheMaxSize`: L1 캐시 최대 항목 수 (1000개)
  - `EnableOpenTelemetry`: OpenTelemetry 계측 활성화
  - `EnableDetailedLogging`: 상세 로깅 활성화

## 요구사항 충족 확인

### 요구사항 1.1 - 기존 기능 호환성

✅ FusionCache 패키지가 설치되어 기존 IIpToNationCache 인터페이스와 호환 가능한 구현체 개발 준비 완료

### 요구사항 1.2 - 캐시 저장 기능

✅ FusionCacheConfig에서 캐시 저장 관련 설정 (DefaultEntryOptions, L1CacheDuration) 정의 완료

### 요구사항 1.3 - 키 형식 호환성

✅ 기존 키 형식 유지를 위한 설정 기반 구조 준비 완료

### 요구사항 1.4 - TTL 호환성

✅ 기존과 동일한 TTL 적용을 위한 시간 설정 옵션들 정의 완료

## 다음 단계

이제 다음 작업들을 진행할 수 있습니다:

1. **Task 2.1**: IpToNationFusionCache 기본 클래스 구조 및 생성자 구현
2. **Task 2.2**: GetAsync 메서드 구현
3. **Task 2.3**: SetAsync 메서드 구현

## 파일 변경 사항

### 수정된 파일

- `Demo.Infra/Demo.Infra.csproj`: FusionCache 패키지 참조 추가

### 새로 생성된 파일

- `Demo.Infra/Configs/FusionCacheConfig.cs`: FusionCache 설정 클래스

## 검증

프로젝트 빌드가 성공적으로 완료되어 패키지 설치와 설정 클래스 생성이 올바르게 이루어졌음을 확인했습니다.

```bash
dotnet build Demo.Infra/Demo.Infra.csproj
# 결과: 성공 빌드(5.5초)
```
