#!/bin/bash

# Rate Limiting 성능 및 부하 테스트 실행 스크립트

echo "=== Rate Limiting Performance and Load Testing ==="
echo "Starting Rate Limiting benchmarks..."

# 프로젝트 디렉토리로 이동
cd "$(dirname "$0")/.."

# 프로젝트 빌드
echo "Building project..."
dotnet build -c Release

if [ $? -ne 0 ]; then
    echo "Build failed. Exiting..."
    exit 1
fi

# Rate Limiting 벤치마크 실행
echo "Running Rate Limiting benchmarks..."
dotnet run -c Release -- ratelimit

# 결과 확인
echo ""
echo "=== Benchmark Results ==="
echo "Results are available in the following locations:"
echo "- BenchmarkDotNet.Artifacts/results/ (detailed results)"
echo "- BenchmarkDotNet.Artifacts/ (logs and reports)"

# 결과 파일 목록 표시
if [ -d "BenchmarkDotNet.Artifacts/results" ]; then
    echo ""
    echo "Generated result files:"
    ls -la BenchmarkDotNet.Artifacts/results/
fi

echo ""
echo "=== Rate Limiting Performance Test Completed ==="