#!/bin/bash

# FusionCache 성능 벤치마크 실행 스크립트

echo "=== FusionCache 성능 벤치마크 테스트 시작 ==="
echo

# 프로젝트 디렉토리로 이동
cd "$(dirname "$0")/../../Demo.Infra.Benchmarks"

# 프로젝트 빌드
echo "프로젝트 빌드 중..."
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo "빌드 실패!"
    exit 1
fi

echo "빌드 완료!"
echo

# 벤치마크 실행
echo "벤치마크 실행 중..."
echo

# 응답 시간 벤치마크
echo "1. 응답 시간 벤치마크 실행..."
dotnet run -c Release -- response

echo
echo "2. 동시성 및 처리량 벤치마크 실행..."
dotnet run -c Release -- concurrency

echo
echo "=== 모든 벤치마크 완료 ==="
echo "결과는 BenchmarkDotNet.Artifacts 디렉토리에서 확인할 수 있습니다."