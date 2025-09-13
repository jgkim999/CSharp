# Task 8: FusionCache 성능 테스트 및 벤치마크 구현

## 개요

FusionCache와 기존 Redis 구현체 간의 성능 비교를 위한 종합적인 벤치마크 테스트를 구현했습니다. BenchmarkDotNet을 사용하여 응답 시간, 동시성, 처리량, 메모리 사용량을 측정합니다.

## 구현된 기능

### 8.1 응답 시간 벤치마크 테스트

#### 구현 파일
- `Demo.Infra.Tests/Benchmarks/IpToNationCacheResponseTimeBenchmark.cs`

#### 측정 시나리오
1. **FusionCache L1 캐시 히트**: 메모리 캐시에서 직접 조회
2. **FusionCache L2 캐시 히트**: Redis에서 조회 후 L1에 저장
3. **기존 Redis 구현체**: 직접 Redis 조회
4. **캐시 미스 시나리오**: 존재하지 않는 데이터 조회
5. **캐시 설정 작업**: 새로운 데이터 저장

#### 주요 특징
- Testcontainers를 사용한 격리된 Redis 환경
- 사전 데이터 로드로 정확한 L1/L2 히트 시나리오 구현
- 메모리 진단 및 성능 순위 측정
- 1000회 반복 실행으로 정확한 성능 측정

### 8.2 동시성 및 처리량 테스트

#### 구현 파일
- `Demo.Infra.Tests/Benchmarks/IpToNationCacheConcurrencyBenchmark.cs`

#### 측정 시나리오
1. **동시 읽기 처리량**: 10개/50개 스레드 동시 요청
2. **캐시 스탬피드 방지**: 동일 키에 대한 20개 동시 요청
3. **혼합 작업**: 읽기(67%) + 쓰기(33%) 작업 조합
4. **메모리 사용량**: 100개 캐시 항목 생성 및 조회

#### 주요 특징
- ThreadingDiagnoser로 스레드 사용량 측정
- ConcurrentBag을 사용한 스레드 안전 결과 수집
- 캐시 스탬피드 방지 효과 검증
- 실제 운영 환경과 유사한 혼합 워크로드 시뮬레이션

## 벤치마크 실행 도구

### BenchmarkRunner 클래스
- `Demo.Infra.Tests/Benchmarks/BenchmarkRunner.cs`
- 개별 벤치마크 또는 전체 벤치마크 실행 지원

### 콘솔 애플리케이션
- `Demo.Infra.Tests/Program.cs`
- 명령줄 인터페이스로 벤치마크 선택 실행

### 실행 스크립트
- `Demo.Infra.Tests/Scripts/run-performance-benchmarks.sh` (Linux/macOS)
- `Demo.Infra.Tests/Scripts/run-performance-benchmarks.ps1` (Windows)

## 사용법

### 명령줄 실행
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
./Scripts/run-performance-benchmarks.sh

# Windows PowerShell
.\Scripts\run-performance-benchmarks.ps1
```

## 벤치마크 설정

### BenchmarkDotNet 구성
- **Warmup**: 2-3회 (환경 안정화)
- **Iteration**: 5-10회 (정확한 측정)
- **Invocation**: 100-1000회 (통계적 유의성)
- **Memory Diagnoser**: 메모리 할당 추적
- **Threading Diagnoser**: 스레드 사용량 추적

### 테스트 환경
- Redis 7-alpine 컨테이너
- 격리된 네트워크 환경
- 일관된 테스트 데이터셋
- 사전 로드된 캐시 데이터

## 예상 성능 결과

### 응답 시간 (예상)
1. **L1 캐시 히트**: < 1μs (가장 빠름)
2. **L2 캐시 히트**: 1-10ms (네트워크 지연 포함)
3. **Redis 직접**: 1-10ms (유사한 성능)
4. **캐시 미스**: 1-10ms (백엔드 조회 없음)

### 동시성 성능 (예상)
1. **FusionCache**: 캐시 스탬피드 방지로 더 안정적
2. **Redis 직접**: 높은 동시성에서 성능 저하 가능
3. **메모리 사용량**: FusionCache가 L1 캐시로 인해 더 높음

## 성능 최적화 포인트

### FusionCache 장점
- L1 캐시로 인한 극도로 빠른 응답 시간
- 캐시 스탬피드 방지로 안정적인 동시성 처리
- 백그라운드 새로고침으로 사용자 경험 향상
- Redis 장애 시 L1 캐시로 서비스 연속성 보장

### 고려사항
- L1 캐시로 인한 메모리 사용량 증가
- 초기 설정 복잡도 증가
- 추가적인 의존성 (FusionCache 패키지)

## 모니터링 및 분석

### 수집되는 메트릭
- **응답 시간**: 평균, 최소, 최대, 백분위수
- **처리량**: 초당 요청 수 (RPS)
- **메모리 할당**: GC 압박, 할당량
- **스레드 사용량**: 동시 스레드 수, 컨텍스트 스위치

### 결과 분석 방법
1. **BenchmarkDotNet 보고서**: HTML/Markdown 형태
2. **성능 비교**: 기존 구현체 대비 개선율
3. **메모리 프로파일링**: 할당 패턴 분석
4. **동시성 분석**: 스레드 경합 및 대기 시간

## 결론

이 벤치마크 구현을 통해 FusionCache의 성능 특성을 정량적으로 측정하고, 기존 Redis 구현체와의 객관적인 비교가 가능합니다. 특히 L1/L2 캐시 계층의 효과와 고급 기능들의 성능 영향을 명확히 파악할 수 있습니다.

## 다음 단계

1. **실제 벤치마크 실행**: 다양한 환경에서 성능 측정
2. **결과 분석**: 성능 개선 포인트 식별
3. **튜닝**: FusionCache 설정 최적화
4. **문서화**: 성능 특성 및 권장사항 정리