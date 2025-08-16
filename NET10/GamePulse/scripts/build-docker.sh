#!/bin/bash

# GamePulse Docker 빌드 스크립트
# 요구사항 1.1, 1.2를 충족하는 최적화된 Docker 이미지 빌드

set -euo pipefail

# ============================================================================
# 설정 변수
# ============================================================================
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
IMAGE_NAME="${IMAGE_NAME:-gamepulse}"
IMAGE_TAG="${IMAGE_TAG:-latest}"
BUILD_CONTEXT="${BUILD_CONTEXT:-$PROJECT_ROOT}"
DOCKERFILE_PATH="${DOCKERFILE_PATH:-$PROJECT_ROOT/Dockerfile}"

# 색상 코드 정의
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ============================================================================
# 유틸리티 함수
# ============================================================================
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 도움말 출력
show_help() {
    cat << EOF
GamePulse Docker 빌드 스크립트

사용법:
    $0 [옵션]

옵션:
    -n, --name NAME         이미지 이름 (기본값: gamepulse)
    -t, --tag TAG          이미지 태그 (기본값: latest)
    -c, --context PATH     빌드 컨텍스트 경로 (기본값: 현재 프로젝트 루트)
    -f, --file PATH        Dockerfile 경로 (기본값: ./Dockerfile)
    --no-cache             캐시 없이 빌드
    --platform PLATFORM    대상 플랫폼 (예: linux/amd64,linux/arm64)
    --push                 빌드 후 레지스트리에 푸시
    --scan                 빌드 후 보안 스캔 실행
    --multi-arch           멀티 아키텍처 빌드
    -h, --help             이 도움말 출력

환경 변수:
    IMAGE_NAME             이미지 이름
    IMAGE_TAG              이미지 태그
    BUILD_CONTEXT          빌드 컨텍스트 경로
    DOCKERFILE_PATH        Dockerfile 경로
    DOCKER_REGISTRY        Docker 레지스트리 URL
    NO_CACHE               캐시 사용 안함 (true/false)

예제:
    # 기본 빌드
    $0

    # 특정 태그로 빌드
    $0 --tag v1.0.0

    # 캐시 없이 빌드
    $0 --no-cache

    # 멀티 아키텍처 빌드
    $0 --multi-arch --platform linux/amd64,linux/arm64

    # 빌드 후 푸시
    $0 --tag v1.0.0 --push
EOF
}

# Docker 설치 확인
check_docker() {
    if ! command -v docker &> /dev/null; then
        log_error "Docker가 설치되지 않았습니다."
        exit 1
    fi

    if ! docker info &> /dev/null; then
        log_error "Docker 데몬이 실행되지 않았습니다."
        exit 1
    fi

    log_info "Docker 버전: $(docker --version)"
}

# BuildKit 활성화 확인
check_buildkit() {
    if [[ "${DOCKER_BUILDKIT:-}" != "1" ]]; then
        log_info "Docker BuildKit 활성화 중..."
        export DOCKER_BUILDKIT=1
    fi
}

# 빌드 컨텍스트 검증
validate_build_context() {
    if [[ ! -d "$BUILD_CONTEXT" ]]; then
        log_error "빌드 컨텍스트 디렉토리가 존재하지 않습니다: $BUILD_CONTEXT"
        exit 1
    fi

    if [[ ! -f "$DOCKERFILE_PATH" ]]; then
        log_error "Dockerfile이 존재하지 않습니다: $DOCKERFILE_PATH"
        exit 1
    fi

    log_info "빌드 컨텍스트: $BUILD_CONTEXT"
    log_info "Dockerfile: $DOCKERFILE_PATH"
}

# 이미지 크기 확인
check_image_size() {
    local image_full_name="$1"
    local size=$(docker images --format "table {{.Size}}" "$image_full_name" | tail -n 1)
    log_info "빌드된 이미지 크기: $size"

    # 이미지 크기가 500MB를 초과하면 경고
    local size_mb=$(docker images --format "{{.Size}}" "$image_full_name" | tail -n 1 | sed 's/MB//' | sed 's/GB/*1000/' | bc 2>/dev/null || echo "0")
    if (( $(echo "$size_mb > 500" | bc -l 2>/dev/null || echo "0") )); then
        log_warning "이미지 크기가 500MB를 초과합니다. 최적화를 고려해보세요."
    fi
}

# 보안 스캔 실행
run_security_scan() {
    local image_full_name="$1"

    log_info "보안 스캔 실행 중..."

    # Docker Scout 사용 (사용 가능한 경우)
    if command -v docker scout &> /dev/null; then
        docker scout cves "$image_full_name" || log_warning "Docker Scout 스캔 실패"
    fi

    # Trivy 사용 (사용 가능한 경우)
    if command -v trivy &> /dev/null; then
        trivy image "$image_full_name" || log_warning "Trivy 스캔 실패"
    fi

    if ! command -v docker scout &> /dev/null && ! command -v trivy &> /dev/null; then
        log_warning "보안 스캔 도구가 설치되지 않았습니다. Docker Scout 또는 Trivy 설치를 권장합니다."
    fi
}

# 멀티 아키텍처 빌드
build_multi_arch() {
    local image_full_name="$1"
    local platform="${PLATFORM:-linux/amd64,linux/arm64}"

    log_info "멀티 아키텍처 빌드 시작 (플랫폼: $platform)"

    # buildx 빌더 생성 (존재하지 않는 경우)
    if ! docker buildx ls | grep -q "gamepulse-builder"; then
        log_info "buildx 빌더 생성 중..."
        docker buildx create --name gamepulse-builder --use
    else
        docker buildx use gamepulse-builder
    fi

    # 빌드 명령어 구성
    local build_cmd="docker buildx build"
    build_cmd+=" --platform $platform"
    build_cmd+=" --file $DOCKERFILE_PATH"
    build_cmd+=" --tag $image_full_name"

    if [[ "${NO_CACHE:-false}" == "true" ]]; then
        build_cmd+=" --no-cache"
    fi

    if [[ "${PUSH_IMAGE:-false}" == "true" ]]; then
        build_cmd+=" --push"
    else
        build_cmd+=" --load"
    fi

    build_cmd+=" $BUILD_CONTEXT"

    log_info "빌드 명령어: $build_cmd"
    eval "$build_cmd"
}

# 일반 빌드
build_single_arch() {
    local image_full_name="$1"

    log_info "단일 아키텍처 빌드 시작"

    # 빌드 명령어 구성
    local build_cmd="docker build"
    build_cmd+=" --file $DOCKERFILE_PATH"
    build_cmd+=" --tag $image_full_name"

    if [[ "${NO_CACHE:-false}" == "true" ]]; then
        build_cmd+=" --no-cache"
    fi

    if [[ -n "${PLATFORM:-}" ]]; then
        build_cmd+=" --platform $PLATFORM"
    fi

    # 빌드 인자 추가
    build_cmd+=" --build-arg BUILDKIT_INLINE_CACHE=1"
    build_cmd+=" --build-arg BUILD_DATE=$(date -u +'%Y-%m-%dT%H:%M:%SZ')"
    build_cmd+=" --build-arg VCS_REF=$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown')"

    build_cmd+=" $BUILD_CONTEXT"

    log_info "빌드 명령어: $build_cmd"
    eval "$build_cmd"
}

# 이미지 푸시
push_image() {
    local image_full_name="$1"

    if [[ "${PUSH_IMAGE:-false}" == "true" ]]; then
        log_info "이미지 푸시 중: $image_full_name"
        docker push "$image_full_name"
        log_success "이미지 푸시 완료"
    fi
}

# ============================================================================
# 메인 함수
# ============================================================================
main() {
    # 명령행 인자 파싱
    while [[ $# -gt 0 ]]; do
        case $1 in
            -n|--name)
                IMAGE_NAME="$2"
                shift 2
                ;;
            -t|--tag)
                IMAGE_TAG="$2"
                shift 2
                ;;
            -c|--context)
                BUILD_CONTEXT="$2"
                shift 2
                ;;
            -f|--file)
                DOCKERFILE_PATH="$2"
                shift 2
                ;;
            --no-cache)
                NO_CACHE="true"
                shift
                ;;
            --platform)
                PLATFORM="$2"
                shift 2
                ;;
            --push)
                PUSH_IMAGE="true"
                shift
                ;;
            --scan)
                RUN_SCAN="true"
                shift
                ;;
            --multi-arch)
                MULTI_ARCH="true"
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                log_error "알 수 없는 옵션: $1"
                show_help
                exit 1
                ;;
        esac
    done

    # 전체 이미지 이름 구성
    local image_full_name="$IMAGE_NAME:$IMAGE_TAG"
    if [[ -n "${DOCKER_REGISTRY:-}" ]]; then
        image_full_name="$DOCKER_REGISTRY/$image_full_name"
    fi

    log_info "GamePulse Docker 빌드 시작"
    log_info "이미지: $image_full_name"

    # 사전 검사
    check_docker
    check_buildkit
    validate_build_context

    # 빌드 시작 시간 기록
    local start_time=$(date +%s)

    # 빌드 실행
    if [[ "${MULTI_ARCH:-false}" == "true" ]]; then
        build_multi_arch "$image_full_name"
    else
        build_single_arch "$image_full_name"

        # 이미지 크기 확인 (단일 아키텍처 빌드에서만)
        check_image_size "$image_full_name"

        # 이미지 푸시
        push_image "$image_full_name"

        # 보안 스캔 실행
        if [[ "${RUN_SCAN:-false}" == "true" ]]; then
            run_security_scan "$image_full_name"
        fi
    fi

    # 빌드 완료 시간 계산
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))

    log_success "빌드 완료! (소요 시간: ${duration}초)"
    log_info "이미지: $image_full_name"

    # 빌드 결과 요약
    echo
    echo "=== 빌드 결과 요약 ==="
    echo "이미지 이름: $image_full_name"
    echo "빌드 시간: ${duration}초"
    if [[ "${MULTI_ARCH:-false}" != "true" ]]; then
        echo "이미지 크기: $(docker images --format "{{.Size}}" "$image_full_name" | head -n 1)"
    fi
    echo "빌드 컨텍스트: $BUILD_CONTEXT"
    echo "Dockerfile: $DOCKERFILE_PATH"
}

# 스크립트 실행
main "$@"
