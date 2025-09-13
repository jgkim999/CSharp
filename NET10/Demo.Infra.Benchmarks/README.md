# FusionCache 성능 벤치마크

이 프로젝트는 FusionCache와 기존 Redis 구현체 간의 성능 비교를 위한 벤치마크 테스트를 제공합니다.

## 벤치마크 종류

### 1. 응답 시간 벤치마크 (IpToNationCacheResponseTimeBenchmark)

다양한 캐시 시나리오에서의 응답 시간을 측정합니다:

- **FusionCache L1 캐시 히트**: 메모리 캐시에서 직접 조회
- **FusionCache L2 캐시 히트**: Redis에서 조회 후 L1에 저장
- **기존 Redis 구현체**: 직접 Redis 조회
- **캐시 미스 시나리오**: 존재하지 않는 데이터 조회
- **캐시 설정 작업**: 새로운 데이터 저장

### 2. 동시성 및 처리량 벤치마크 (IpToNationCacheConcurrencyBenchmark)

동시성 환경에서의 성능과 처리량을 측정합니다:

- **동시 읽기 처리량**: 10개/50개 스레드 동시 요청
- **캐시 스탬피드 방지**: 동일 키에 대한 20개 동시 요청
- **혼합 작업**: 읽기(67%) + 쓰기(33%) 작업 조합
- **메모리 사용량**: 100개 캐시 항목 생성 및 조회

## 실행 방법

### 직접 실행

```bash
# 응답 시간 벤치마크만 실행
dotnet run -c Release -- response

# 동시성 벤치마크만 실행
dotnet run -c Release -- concurrency

# 모든 벤치마크 실행
dotnet run -c Release -- all
```

### 스크립트 실행

```bash
# Linux/macOS
../Demo.Infra.Tests/Scripts/run-performance-benchmarks.sh

# Windows PowerShell
..\Demo.Infra.Tests\Scripts\run-performance-benchmarks.ps1
```

## 결과 확인

벤치마크 실행 후 결과는 다음 위치에서 확인할 수 있습니다:

- `BenchmarkDotNet.Artifacts/results/` - HTML 및 CSV 형태의 상세 결과
- 콘솔 출력 - 요약된 성능 지표

## 벤치마크 설정

### BenchmarkDotNet 구성

- **Warmup**: 2-3회 (환경 안정화)
- **Iteration**: 5-10회 (정확한 측정)
- **Invocation**: 100-1000회 (통계적 유의성)
- **Memory Diagnoser**: 메모리 할당 추적
- **Threading Diagnoser**: 스레드 사용량 추적

### 테스트 환경

- Redis 7-alpine 컨테이너 (Testcontainers 사용)
- 격리된 네트워크 환경
- 일관된 테스트 데이터셋
- 사전 로드된 캐시 데이터

## 예상 성능 특성

### FusionCache 장점

- **L1 캐시 히트**: 극도로 빠른 응답 시간 (< 1μs)
- **캐시 스탬피드 방지**: 안정적인 동시성 처리
- **백그라운드 새로고침**: 사용자 경험 향상
- **페일오버**: Redis 장애 시 서비스 연속성

### 고려사항

- **메모리 사용량**: L1 캐시로 인한 메모리 사용량 증가
- **복잡도**: 초기 설정 및 구성의 복잡도
- **의존성**: 추가적인 패키지 의존성

## 성능 분석

벤치마크 결과를 통해 다음을 분석할 수 있습니다:

1. **응답 시간 개선**: L1 캐시 히트 시 성능 향상 정도
2. **동시성 처리**: 높은 부하에서의 안정성
3. **메모리 효율성**: 메모리 사용량 대비 성능 이득
4. **스케일링**: 동시 사용자 증가에 따른 성능 변화

## 문제 해결

### Docker 관련 문제

벤치마크는 Testcontainers를 사용하여 Redis 컨테이너를 실행합니다. Docker가 설치되어 있고 실행 중인지 확인하세요.

### 메모리 부족

대량의 동시성 테스트로 인해 메모리 부족이 발생할 수 있습니다. 시스템 메모리를 확인하고 필요시 테스트 규모를 조정하세요.

### 네트워크 지연

네트워크 환경에 따라 Redis 연결 성능이 달라질 수 있습니다. 일관된 결과를 위해 로컬 환경에서 실행하는 것을 권장합니다.