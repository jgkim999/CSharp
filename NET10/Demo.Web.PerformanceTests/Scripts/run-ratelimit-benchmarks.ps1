# Rate Limiting 성능 및 부하 테스트 실행 PowerShell 스크립트

Write-Host "=== Rate Limiting Performance and Load Testing ===" -ForegroundColor Green
Write-Host "Starting Rate Limiting benchmarks..." -ForegroundColor Yellow

# 프로젝트 디렉토리로 이동
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Split-Path -Parent $scriptPath
Set-Location $projectPath

# 프로젝트 빌드
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Exiting..." -ForegroundColor Red
    exit 1
}

# Rate Limiting 벤치마크 실행
Write-Host "Running Rate Limiting benchmarks..." -ForegroundColor Yellow
dotnet run -c Release -- ratelimit

# 결과 확인
Write-Host ""
Write-Host "=== Benchmark Results ===" -ForegroundColor Green
Write-Host "Results are available in the following locations:" -ForegroundColor White
Write-Host "- BenchmarkDotNet.Artifacts/results/ (detailed results)" -ForegroundColor Cyan
Write-Host "- BenchmarkDotNet.Artifacts/ (logs and reports)" -ForegroundColor Cyan

# 결과 파일 목록 표시
if (Test-Path "BenchmarkDotNet.Artifacts/results") {
    Write-Host ""
    Write-Host "Generated result files:" -ForegroundColor White
    Get-ChildItem "BenchmarkDotNet.Artifacts/results" | Format-Table Name, Length, LastWriteTime
}

Write-Host ""
Write-Host "=== Rate Limiting Performance Test Completed ===" -ForegroundColor Green