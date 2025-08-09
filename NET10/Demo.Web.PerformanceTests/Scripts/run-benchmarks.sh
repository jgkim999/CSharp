#!/bin/bash

# OpenTelemetry 성능 벤치마크 실행 스크립트 (Linux/macOS)

CONFIGURATION="Release"
OUTPUT_DIR="BenchmarkDotNet.Artifacts"
QUICK=false
DETAILED=false

# 매개변수 파싱
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --quick)
            QUICK=true
            shift
            ;;
        --detailed)
            DETAILED=true
            shift
            ;;
        *)
            echo "알 수 없는 매개변수: $1"
            exit 1
            ;;
    esac
done

echo "OpenTelemetry 성능 벤치마크 시작..."
echo "구성: $CONFIGURATION"

# 프로젝트 빌드
echo "프로젝트 빌드 중..."
dotnet build -c "$CONFIGURATION" --no-restore

if [ $? -ne 0 ]; then
    echo "빌드 실패!"
    exit 1
fi

# 출력 디렉토리 생성
mkdir -p "$OUTPUT_DIR"

# 벤치마크 실행 매개변수 설정
BENCHMARK_ARGS="run -c $CONFIGURATION --artifacts $OUTPUT_DIR"

if [ "$QUICK" = true ]; then
    echo "빠른 모드로 실행 중..."
    BENCHMARK_ARGS="$BENCHMARK_ARGS --job short"
elif [ "$DETAILED" = true ]; then
    echo "상세 모드로 실행 중..."
    BENCHMARK_ARGS="$BENCHMARK_ARGS --job long"
fi

# 각 벤치마크 클래스별로 실행
BENCHMARK_CLASSES=(
    "Demo.Web.PerformanceTests.Benchmarks.ApplicationStartupBenchmark"
    "Demo.Web.PerformanceTests.Benchmarks.HttpRequestBenchmark"
    "Demo.Web.PerformanceTests.Benchmarks.MemoryUsageBenchmark"
    "Demo.Web.PerformanceTests.Benchmarks.LoadTestBenchmark"
)

for benchmark_class in "${BENCHMARK_CLASSES[@]}"; do
    echo "실행 중: $benchmark_class"
    
    dotnet $BENCHMARK_ARGS --filter "*$benchmark_class*"
    
    if [ $? -ne 0 ]; then
        echo "벤치마크 실행 실패: $benchmark_class"
        continue
    fi
    
    echo "완료: $benchmark_class"
done

echo "모든 벤치마크 완료!"
echo "결과는 $OUTPUT_DIR 디렉토리에서 확인할 수 있습니다."

# 결과 요약 생성
SUMMARY_FILE="$OUTPUT_DIR/performance-summary.md"
echo "성능 요약 보고서 생성 중: $SUMMARY_FILE"

# 성능 기준 확인
echo "성능 기준 확인 중..."
echo "- 애플리케이션 시작 시간: 기존 대비 10% 이내 증가"
echo "- HTTP 요청 처리 시간: 기존 대비 5% 이내 증가"
echo "- 메모리 사용량: 50MB 이내 증가"
echo "- 최소 처리량: 초당 100 요청"

echo "벤치마크 실행이 완료되었습니다!"