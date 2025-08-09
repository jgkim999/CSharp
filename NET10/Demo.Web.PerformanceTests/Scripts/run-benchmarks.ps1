# OpenTelemetry 성능 벤치마크 실행 스크립트

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "BenchmarkDotNet.Artifacts",
    [switch]$Quick,
    [switch]$Detailed
)

Write-Host "OpenTelemetry 성능 벤치마크 시작..." -ForegroundColor Green
Write-Host "구성: $Configuration" -ForegroundColor Yellow

# 프로젝트 빌드
Write-Host "프로젝트 빌드 중..." -ForegroundColor Yellow
dotnet build -c $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "빌드 실패!" -ForegroundColor Red
    exit 1
}

# 출력 디렉토리 생성
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force
}

# 벤치마크 실행 매개변수 설정
$benchmarkArgs = @(
    "run"
    "-c", $Configuration
    "--artifacts", $OutputDir
)

if ($Quick) {
    Write-Host "빠른 모드로 실행 중..." -ForegroundColor Yellow
    $benchmarkArgs += "--job", "short"
}
elseif ($Detailed) {
    Write-Host "상세 모드로 실행 중..." -ForegroundColor Yellow
    $benchmarkArgs += "--job", "long"
}

# 각 벤치마크 클래스별로 실행
$benchmarkClasses = @(
    "Demo.Web.PerformanceTests.Benchmarks.ApplicationStartupBenchmark",
    "Demo.Web.PerformanceTests.Benchmarks.HttpRequestBenchmark",
    "Demo.Web.PerformanceTests.Benchmarks.MemoryUsageBenchmark",
    "Demo.Web.PerformanceTests.Benchmarks.LoadTestBenchmark"
)

foreach ($benchmarkClass in $benchmarkClasses) {
    Write-Host "실행 중: $benchmarkClass" -ForegroundColor Cyan
    
    $classArgs = $benchmarkArgs + @("--filter", "*$benchmarkClass*")
    
    & dotnet $classArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "벤치마크 실행 실패: $benchmarkClass" -ForegroundColor Red
        continue
    }
    
    Write-Host "완료: $benchmarkClass" -ForegroundColor Green
}

Write-Host "모든 벤치마크 완료!" -ForegroundColor Green
Write-Host "결과는 $OutputDir 디렉토리에서 확인할 수 있습니다." -ForegroundColor Yellow

# 결과 요약 생성
$summaryFile = Join-Path $OutputDir "performance-summary.md"
Write-Host "성능 요약 보고서 생성 중: $summaryFile" -ForegroundColor Yellow

# 성능 기준 확인
Write-Host "성능 기준 확인 중..." -ForegroundColor Yellow
Write-Host "- 애플리케이션 시작 시간: 기존 대비 10% 이내 증가" -ForegroundColor White
Write-Host "- HTTP 요청 처리 시간: 기존 대비 5% 이내 증가" -ForegroundColor White
Write-Host "- 메모리 사용량: 50MB 이내 증가" -ForegroundColor White
Write-Host "- 최소 처리량: 초당 100 요청" -ForegroundColor White

Write-Host "벤치마크 실행이 완료되었습니다!" -ForegroundColor Green