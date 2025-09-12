# FusionCache 성능 벤치마크 실행 스크립트 (PowerShell)

Write-Host "=== FusionCache 성능 벤치마크 테스트 시작 ===" -ForegroundColor Green
Write-Host

# 프로젝트 디렉토리로 이동
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent (Split-Path -Parent $scriptPath)
$projectPath = Join-Path $rootPath "Demo.Infra.Benchmarks"
Set-Location $projectPath

# 프로젝트 빌드
Write-Host "프로젝트 빌드 중..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "빌드 실패!" -ForegroundColor Red
    exit 1
}

Write-Host "빌드 완료!" -ForegroundColor Green
Write-Host

# 벤치마크 실행
Write-Host "벤치마크 실행 중..." -ForegroundColor Yellow
Write-Host

# 응답 시간 벤치마크
Write-Host "1. 응답 시간 벤치마크 실행..." -ForegroundColor Cyan
dotnet run -c Release -- response

Write-Host
Write-Host "2. 동시성 및 처리량 벤치마크 실행..." -ForegroundColor Cyan
dotnet run -c Release -- concurrency

Write-Host
Write-Host "=== 모든 벤치마크 완료 ===" -ForegroundColor Green
Write-Host "결과는 BenchmarkDotNet.Artifacts 디렉토리에서 확인할 수 있습니다." -ForegroundColor Yellow