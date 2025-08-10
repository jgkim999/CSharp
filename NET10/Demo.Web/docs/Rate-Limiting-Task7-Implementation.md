# Rate Limiting 성능 및 부하 테스트 구현 문서

## 개요

이 문서는 Rate Limiting 기능의 성능 및 부하 테스트 구현에 대한 상세한 내용을 다룹니다. 구현된 테스트는 Rate Limiting이 응답 시간에 미치는 영향을 측정하고, 다수의 동시 요청에 대한 동작을 확인하며, 메모리 사용량을 모니터링합니다.

## 구현된 테스트 항목

### 1. 응답 시간 영향 측정 테스트 (RateLimit_ResponseTime_Impact_Test)

**목적**: Rate Limiting이 응답 시간에 미치는 영향을 측정

**구현 내용**:

- 서로 다른 IP로 10개 요청을 보내어 정상 응답 시간 측정
- 동일한 IP로 15개 요청을 보내어 Rate Limit 상황에서의 응답 시간 측정
- 평균 응답 시간 비교 및 성능 영향 분석

**검증 기준**:

- 평균 응답 시간이 1초 미만
- Rate Limit 응답 시간이 정상 응답 시간의 5배 이내

### 2. 동시 요청 처리 테스트 (RateLimit_ConcurrentRequests_Test)

**목적**: 다수의 동시 요청에 대한 Rate Limiting 동작 확인

**구현 내용**:

- 20개의 동시 요청을 서로 다른 IP로 전송
- 각 요청의 응답 시간과 상태 코드 수집
- 동시 요청 처리 성능 분석

**검증 기준**:

- 평균 응답 시간이 2초 미만
- 모든 요청이 정상적으로 처리됨

### 3. 메모리 사용량 모니터링 테스트 (RateLimit_MemoryUsage_Monitoring_Test)

**목적**: Rate Limiting 기능의 메모리 사용량 모니터링

**구현 내용**:

- 50개의 서로 다른 IP로 각각 2개씩 요청 전송
- 테스트 전후 메모리 사용량 측정
- 메모리 스냅샷을 통한 사용량 추적

**검증 기준**:

- 총 메모리 사용량이 50MB 미만
- 모든 요청이 정상적으로 처리됨

### 4. 장시간 실행 성능 테스트 (RateLimit_LongRunning_Performance_Test)

**목적**: 장시간 실행 시나리오에서의 성능 검증

**구현 내용**:

- 15초간 지속적으로 요청 전송
- 매번 새로운 IP를 사용하여 Rate Limit 회피
- 응답 시간, 처리량, 메모리 사용량 모니터링

**검증 기준**:

- 평균 응답 시간이 2초 미만
- 초당 요청 수가 0.5 이상
- 메모리 사용량이 100MB 미만

### 5. 성능 비교 테스트 (RateLimit_Performance_Comparison_Test)

**목적**: Rate Limiting 활성화/비활성화 상태의 성능 비교

**구현 내용**:

- Rate Limiting 활성화된 상태에서 20개 요청 성능 측정
- Rate Limiting 비활성화된 상태에서 20개 요청 성능 측정
- 두 상태의 성능 차이 분석

**검증 기준**:

- 각 상태에서 평균 응답 시간이 2초 미만
- 성능 차이가 합리적인 범위 내

## 성능 테스트 지원 클래스

### 1. PerformanceTestModels.cs

공통으로 사용되는 모델 클래스들을 정의:

- `RequestResult`: 개별 요청의 결과 정보
- `LoadTestResult`: 부하 테스트 전체 결과 정보
- `MemoryTestResult`: 메모리 테스트 결과 정보

### 2. RateLimitingPerformanceAnalyzer.cs

성능 테스트 결과 분석을 위한 헬퍼 클래스:

- `AnalyzeResults()`: 성능 테스트 결과 분석
- `AnalyzeMemoryResults()`: 메모리 사용량 결과 분석
- `SaveReportToFileAsync()`: 결과를 JSON 파일로 저장
- `PrintReport()`: 콘솔에 결과 출력
- `RunBenchmarkAsync()`: 벤치마크 실행 및 결과 분석

## BenchmarkDotNet 성능 테스트

### 1. RateLimitingPerformanceBenchmark.cs

Rate Limiting의 성능 영향을 정밀하게 측정:

- 단일 요청 성능 (Rate Limiting 활성화/비활성화)
- 연속 요청 성능 비교
- Rate Limit 초과 상황에서의 성능

### 2. RateLimitingMemoryBenchmark.cs

메모리 사용량 벤치마크:

- 고유 IP별 메모리 사용량 측정
- 동일 IP 반복 요청 시 메모리 사용량
- 메모리 누수 탐지
- 장시간 실행 시나리오 메모리 모니터링

### 3. RateLimitingLoadTestBenchmark.cs

부하 테스트 벤치마크:

- 동시 요청 처리 성능
- Rate Limit 초과 시나리오
- 메모리 집약적 시나리오

## 테스트 실행 방법

### 통합 테스트 실행

```bash
# 모든 Rate Limiting 성능 테스트 실행
dotnet test Demo.Web.IntegrationTests --filter "RateLimitingPerformanceIntegrationTests"

# 특정 테스트 실행
dotnet test Demo.Web.IntegrationTests --filter "RateLimit_ResponseTime_Impact_Test"
```

### BenchmarkDotNet 실행

```bash
# 성능 벤치마크 실행
dotnet run -c Release -p Demo.Web.PerformanceTests -- ratelimit

# 특정 벤치마크 실행
dotnet run -c Release -p Demo.Web.PerformanceTests -- --filter "*RateLimitingPerformanceBenchmark*"
```

### 스크립트를 통한 실행

```bash
# Linux/macOS
./NET10/Demo.Web.PerformanceTests/Scripts/run-ratelimit-benchmarks.sh

# Windows
.\NET10\Demo.Web.PerformanceTests\Scripts\run-ratelimit-benchmarks.ps1
```

## 성능 테스트 결과 분석

### 주요 메트릭

1. **응답 시간 (Response Time)**
   - 평균, 최대, 최소 응답 시간
   - Rate Limiting 활성화/비활성화 상태 비교

2. **처리량 (Throughput)**
   - 초당 요청 수 (RPS)
   - 동시 요청 처리 능력

3. **메모리 사용량 (Memory Usage)**
   - 총 메모리 사용량
   - IP당 메모리 사용량
   - 메모리 누수 여부

4. **성공률 (Success Rate)**
   - 성공한 요청 비율
   - Rate Limit된 요청 비율
   - 실패한 요청 비율

### 성능 기준

- **응답 시간**: 평균 1-2초 미만
- **처리량**: 초당 0.5개 이상의 요청 처리
- **메모리 사용량**: IP당 1KB 미만, 총 50-100MB 미만
- **성공률**: 99% 이상 (Rate Limit 제외)

## 문제 해결

### 일반적인 문제

1. **403 Forbidden 오류**
   - 원인: Rate Limiting 설정이 제대로 주입되지 않음
   - 해결: TestWebApplicationFactory에서 RateLimitConfig 설정 확인

2. **메모리 사용량 초과**
   - 원인: 많은 수의 고유 IP로 인한 메모리 사용량 증가
   - 해결: 테스트 규모 조정 또는 메모리 정리 로직 추가

3. **데이터베이스 제약 조건 위반**
   - 원인: 동일한 이메일로 중복 사용자 생성 시도
   - 해결: 각 요청마다 고유한 이메일 사용

### 성능 최적화 권장사항

1. **Rate Limiting 설정 최적화**
   - 적절한 Hit Limit과 Duration 설정
   - 메모리 기반 저장소의 효율적 사용

2. **테스트 환경 최적화**
   - 충분한 메모리와 CPU 리소스 확보
   - 네트워크 지연 최소화

3. **모니터링 강화**
   - 실시간 메트릭 수집
   - 알림 시스템 구축

## 결론

구현된 성능 및 부하 테스트는 Rate Limiting 기능의 다양한 측면을 종합적으로 검증합니다. 이를 통해 시스템의 성능 특성을 이해하고, 운영 환경에서의 안정성을 보장할 수 있습니다.

테스트 결과를 바탕으로 Rate Limiting 설정을 최적화하고, 필요에 따라 추가적인 성능 개선 작업을 수행할 수 있습니다.