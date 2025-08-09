# Demo.Web 성능 벤치마크 테스트

이 프로젝트는 OpenTelemetry 도입으로 인한 성능 영향을 측정하기 위한 벤치마크 테스트를 포함합니다.

## 개요

OpenTelemetry 도입 시 다음 성능 기준을 만족해야 합니다:

- 애플리케이션 시작 시간이 기존 대비 10% 이내 증가
- HTTP 요청 처리 시간이 기존 대비 5% 이내 증가
- 메모리 사용량이 50MB 이내 증가
- 최소 초당 100 요청 처리 가능

## 벤치마크 테스트 항목

### 1. ApplicationStartupBenchmark

- OpenTelemetry 유무에 따른 애플리케이션 시작 시간 비교
- 개발/프로덕션 환경별 시작 시간 측정

### 2. HttpRequestBenchmark

- 단순 GET 요청 처리 성능
- POST 요청 처리 성능
- 대용량 페이로드 처리 성능
- 연속 요청 처리 성능

### 3. MemoryUsageBenchmark

- 기본 메모리 사용량 비교
- 메모리 누수 테스트
- 프로세스 메모리 사용량 측정
- 가비지 컬렉션 영향 분석

### 4. LoadTestBenchmark

- 다양한 동시성 수준에서의 성능 측정
- 혼합 워크로드 테스트
- 처리량 및 응답 시간 분석

## 실행 방법

### 전체 벤치마크 실행

#### Windows (PowerShell)

```powershell
.\Scripts\run-benchmarks.ps1
```

#### Linux/macOS (Bash)

```bash
./Scripts/run-benchmarks.sh
```

### 개별 벤치마크 실행

```bash
# 프로젝트 빌드
dotnet build -c Release

# 특정 벤치마크 실행
dotnet run -c Release --filter "*ApplicationStartupBenchmark*"
dotnet run -c Release --filter "*HttpRequestBenchmark*"
dotnet run -c Release --filter "*MemoryUsageBenchmark*"
dotnet run -c Release --filter "*LoadTestBenchmark*"
```

### 빠른 테스트 (개발용)

```bash
# Windows
.\Scripts\run-benchmarks.ps1 -Quick

# Linux/macOS
./Scripts/run-benchmarks.sh --quick
```

### 상세 분석 (CI/CD용)

```bash
# Windows
.\Scripts\run-benchmarks.ps1 -Detailed

# Linux/macOS
./Scripts/run-benchmarks.sh --detailed
```

## 결과 분석

벤치마크 실행 후 `BenchmarkDotNet.Artifacts` 디렉토리에 다음 파일들이 생성됩니다:

- **HTML 보고서**: 시각적인 결과 분석
- **Markdown 보고서**: GitHub에서 보기 좋은 형태
- **CSV 데이터**: 추가 분석을 위한 원시 데이터
- **JSON 데이터**: 프로그래밍 방식 분석용

## 성능 기준 검증

각 벤치마크는 다음 기준으로 평가됩니다:

### 시작 시간 기준

- **통과**: OpenTelemetry 포함 시작 시간이 기준 대비 10% 이내 증가
- **실패**: 10% 초과 증가

### 요청 처리 기준

- **통과**: OpenTelemetry 포함 처리 시간이 기준 대비 5% 이내 증가
- **실패**: 5% 초과 증가

### 메모리 사용량 기준

- **통과**: 메모리 사용량이 50MB 이내 증가
- **실패**: 50MB 초과 증가

### 처리량 기준

- **통과**: 최소 초당 100 요청 처리
- **실패**: 100 req/s 미만

## 문제 해결

### 일반적인 문제

1. **빌드 오류**
   ```bash
   dotnet restore
   dotnet build -c Release
   ```

2. **권한 오류 (Linux/macOS)**
   ```bash
   chmod +x Scripts/run-benchmarks.sh
   ```

3. **메모리 부족**
   - 벤치마크 반복 횟수 줄이기
   - 시스템 메모리 확인

### 성능 최적화 팁

1. **시스템 준비**
   - 다른 애플리케이션 종료
   - 안정적인 전원 공급
   - 충분한 메모리 확보

2. **정확한 측정**
   - Release 모드에서 실행
   - 여러 번 실행하여 평균값 사용
   - 시스템 부하가 낮을 때 실행

## CI/CD 통합

### GitHub Actions 예제

```yaml
name: Performance Benchmarks

on:
  pull_request:
    branches: [ main ]

jobs:
  benchmark:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore NET10/Demo.Web.PerformanceTests
    
    - name: Run benchmarks
      run: |
        cd NET10/Demo.Web.PerformanceTests
        ./Scripts/run-benchmarks.sh --quick
    
    - name: Upload results
      uses: actions/upload-artifact@v3
      with:
        name: benchmark-results
        path: NET10/Demo.Web.PerformanceTests/BenchmarkDotNet.Artifacts/
```

## 참고 자료

- [BenchmarkDotNet 문서](https://benchmarkdotnet.org/)
- [OpenTelemetry .NET 성능 가이드](https://opentelemetry.io/docs/instrumentation/net/performance/)
- [ASP.NET Core 성능 모범 사례](https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices)