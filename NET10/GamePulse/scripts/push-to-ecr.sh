#!/bin/bash

# GamePulse ECR 푸시 자동화 스크립트
# 요구사항 1.3: Docker 이미지 ECR 푸시 자동화 및 시맨틱 버전 태깅

set -euo pipefail

# ============================================================================
# 설정 변수
# ============================================================================
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# AWS 및 ECR 설정
AWS_REGION="${AWS_REGION:-ap-northeast-2}"
AWS_ACCOUNT_ID="${AWS_ACCOUNT_ID:-}"
ECR_REPOSITORY="${ECR_REPOSITORY:-gamepulse}"
IMAGE_NAME="${IMAGE_NAME:-gamepulse}"

# 버전 관리
VERSION_FILE="${VERSION_FILE:-$PROJECT_ROOT/version.txt}"
SEMANTIC_VERSION="${SEMANTIC_VERSION:-}"
AUTO_INCREMENT="${AUTO_INCREMENT:-patch}"

# 빌드 설정
BUILD_CONTEXT="${BUILD_CONTEXT:-$PROJECT_ROOT}"
DOCKERFILE_PATH="${DOCKERFILE_PATH:-$PROJECT_ROOT/Dockerfile}"
PLATFORM="${PLATFORM:-linux/amd64}"

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
GamePulse ECR 푸시 자동화 스크립트

사용법:
    $0 [옵션]

옵션:
    -r, --region REGION         AWS 리전 (기본값: ap-northeast-2)
    -a, --account-id ID         AWS 계정 ID
    -e, --ecr-repo REPO         ECR 리포지토리 이름 (기본값: gamepulse)
    -v, --version VERSION       시맨틱 버전 (예: 1.2.3)
    -i, --increment TYPE        버전 자동 증가 타입 (major|minor|patch) (기본값: patch)
    --platform PLATFORM        대상 플랫폼 (기본값: linux/amd64)
    --multi-arch               멀티 아키텍처 빌드 및 푸시
    --latest                   latest 태그도 함께 푸시
    --scan                     푸시 후 ECR 이미지 스캔 실행
    --force                    기존 태그 덮어쓰기 허용
    --dry-run                  실제 푸시 없이 시뮬레이션만 실행
    -h, --help                 이 도움말 출력

환경 변수:
    AWS_REGION                 AWS 리전
    AWS_ACCOUNT_ID             AWS 계정 ID
    ECR_REPOSITORY             ECR 리포지토리 이름
    SEMANTIC_VERSION           시맨틱 버전
    AUTO_INCREMENT             자동 증가 타입

예제:
    # 기본 푸시 (patch 버전 자동 증가)
    $0

    # 특정 버전으로 푸시
    $0 --version 1.2.3

    # minor 버전 증가
    $0 --increment minor

    # 멀티 아키텍처 빌드 및 푸시
    $0 --multi-arch --latest

    # 드라이런 모드
    $0 --dry-run --version 1.2.3
EOF
}

# AWS CLI 설치 확인
check_aws_cli() {
    if ! command -v aws &> /dev/null; then
        log_error "AWS CLI가 설치되지 않았습니다."
        exit 1
    fi

    log_info "AWS CLI 버전: $(aws --version)"
}

# AWS 자격 증명 확인
check_aws_credentials() {
    if ! aws sts get-caller-identity &> /dev/null; then
        log_error "AWS 자격 증명이 설정되지 않았습니다."
        log_info "다음 중 하나의 방법으로 자격 증명을 설정하세요:"
        log_info "1. aws configure"
        log_info "2. AWS_ACCESS_KEY_ID 및 AWS_SECRET_ACCESS_KEY 환경 변수"
        log_info "3. IAM 역할 (EC2/ECS에서 실행 시)"
        exit 1
    fi

    # AWS 계정 ID 자동 감지
    if [[ -z "$AWS_ACCOUNT_ID" ]]; then
        AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
        log_info "AWS 계정 ID 자동 감지: $AWS_ACCOUNT_ID"
    fi
}

# ECR 리포지토리 존재 확인
check_ecr_repository() {
    log_info "ECR 리포지토리 확인 중: $ECR_REPOSITORY"

    if ! aws ecr describe-repositories --repository-names "$ECR_REPOSITORY" --region "$AWS_REGION" &> /dev/null; then
        log_warning "ECR 리포지토리가 존재하지 않습니다: $ECR_REPOSITORY"
        log_info "리포지토리 생성 중..."

        aws ecr create-repository \
            --repository-name "$ECR_REPOSITORY" \
            --region "$AWS_REGION" \
            --image-scanning-configuration scanOnPush=true \
            --encryption-configuration encryptionType=AES256

        log_success "ECR 리포지토리 생성 완료: $ECR_REPOSITORY"
    else
        log_info "ECR 리포지토리 확인됨: $ECR_REPOSITORY"
    fi
}

# ECR 로그인
ecr_login() {
    log_info "ECR 로그인 중..."

    aws ecr get-login-password --region "$AWS_REGION" | \
        docker login --username AWS --password-stdin "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com"

    log_success "ECR 로그인 완료"
}

# 현재 버전 읽기
read_current_version() {
    if [[ -f "$VERSION_FILE" ]]; then
        cat "$VERSION_FILE"
    else
        echo "0.0.0"
    fi
}

# 버전 파일 저장
save_version() {
    local version="$1"
    echo "$version" > "$VERSION_FILE"
    log_info "버전 파일 저장: $version"
}

# 시맨틱 버전 증가
increment_version() {
    local current_version="$1"
    local increment_type="$2"

    # 버전 파싱
    local major minor patch
    IFS='.' read -r major minor patch <<< "$current_version"

    case "$increment_type" in
        major)
            major=$((major + 1))
            minor=0
            patch=0
            ;;
        minor)
            minor=$((minor + 1))
            patch=0
            ;;
        patch)
            patch=$((patch + 1))
            ;;
        *)
            log_error "잘못된 증가 타입: $increment_type (major|minor|patch)"
            exit 1
            ;;
    esac

    echo "$major.$minor.$patch"
}

# Git 태그 생성
create_git_tag() {
    local version="$1"
    local tag_name="v$version"

    if git rev-parse --git-dir > /dev/null 2>&1; then
        if git tag -l | grep -q "^$tag_name$"; then
            log_warning "Git 태그가 이미 존재합니다: $tag_name"
        else
            log_info "Git 태그 생성 중: $tag_name"
            git tag -a "$tag_name" -m "Release version $version"
            log_success "Git 태그 생성 완료: $tag_name"
        fi
    else
        log_warning "Git 리포지토리가 아닙니다. Git 태그를 생성하지 않습니다."
    fi
}

# 이미지 태그 존재 확인
check_image_tag_exists() {
    local tag="$1"
    local ecr_uri="$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY"

    if aws ecr describe-images \
        --repository-name "$ECR_REPOSITORY" \
        --image-ids imageTag="$tag" \
        --region "$AWS_REGION" &> /dev/null; then
        return 0  # 태그 존재
    else
        return 1  # 태그 없음
    fi
}

# Docker 이미지 빌드
build_image() {
    local version="$1"
    local ecr_uri="$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY"

    log_info "Docker 이미지 빌드 시작"
    log_info "버전: $version"
    log_info "ECR URI: $ecr_uri"

    # 빌드 스크립트 실행
    local build_script="$SCRIPT_DIR/build-docker.sh"
    if [[ -f "$build_script" ]]; then
        log_info "기존 빌드 스크립트 사용: $build_script"

        # 환경 변수 설정
        export IMAGE_NAME="$ecr_uri"
        export IMAGE_TAG="$version"
        export DOCKER_REGISTRY=""
        export BUILD_CONTEXT="$BUILD_CONTEXT"
        export DOCKERFILE_PATH="$DOCKERFILE_PATH"

        if [[ "${MULTI_ARCH:-false}" == "true" ]]; then
            "$build_script" --multi-arch --platform "$PLATFORM"
        else
            "$build_script" --tag "$version"
        fi
    else
        log_info "직접 Docker 빌드 실행"

        # 빌드 명령어 구성
        local build_cmd="docker build"
        build_cmd+=" --file $DOCKERFILE_PATH"
        build_cmd+=" --tag $ecr_uri:$version"
        build_cmd+=" --platform $PLATFORM"

        # 빌드 인자 추가
        build_cmd+=" --build-arg BUILD_DATE=$(date -u +'%Y-%m-%dT%H:%M:%SZ')"
        build_cmd+=" --build-arg VCS_REF=$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown')"
        build_cmd+=" --build-arg VERSION=$version"

        build_cmd+=" $BUILD_CONTEXT"

        log_info "빌드 명령어: $build_cmd"
        eval "$build_cmd"
    fi

    log_success "Docker 이미지 빌드 완료"
}

# 이미지 태깅
tag_image() {
    local version="$1"
    local ecr_uri="$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY"

    # latest 태그 추가
    if [[ "${TAG_LATEST:-false}" == "true" ]]; then
        log_info "latest 태그 추가 중..."
        docker tag "$ecr_uri:$version" "$ecr_uri:latest"
    fi

    # 추가 태그들
    local git_commit=$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown')
    if [[ "$git_commit" != "unknown" ]]; then
        log_info "Git 커밋 태그 추가 중: $git_commit"
        docker tag "$ecr_uri:$version" "$ecr_uri:$git_commit"
    fi

    # 날짜 태그
    local date_tag=$(date +%Y%m%d)
    log_info "날짜 태그 추가 중: $date_tag"
    docker tag "$ecr_uri:$version" "$ecr_uri:$date_tag"
}

# 이미지 푸시
push_image() {
    local version="$1"
    local ecr_uri="$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY"

    if [[ "${DRY_RUN:-false}" == "true" ]]; then
        log_info "[DRY RUN] 이미지 푸시 시뮬레이션"
        log_info "[DRY RUN] 푸시할 이미지: $ecr_uri:$version"
        if [[ "${TAG_LATEST:-false}" == "true" ]]; then
            log_info "[DRY RUN] 푸시할 이미지: $ecr_uri:latest"
        fi
        return 0
    fi

    log_info "이미지 푸시 시작: $ecr_uri:$version"

    # 메인 버전 태그 푸시
    docker push "$ecr_uri:$version"

    # latest 태그 푸시
    if [[ "${TAG_LATEST:-false}" == "true" ]]; then
        log_info "latest 태그 푸시 중..."
        docker push "$ecr_uri:latest"
    fi

    # 추가 태그들 푸시
    local git_commit=$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown')
    if [[ "$git_commit" != "unknown" ]]; then
        docker push "$ecr_uri:$git_commit"
    fi

    local date_tag=$(date +%Y%m%d)
    docker push "$ecr_uri:$date_tag"

    log_success "이미지 푸시 완료"
}

# ECR 이미지 스캔 실행
run_ecr_scan() {
    local version="$1"

    if [[ "${RUN_SCAN:-false}" == "true" ]]; then
        log_info "ECR 이미지 스캔 시작..."

        aws ecr start-image-scan \
            --repository-name "$ECR_REPOSITORY" \
            --image-id imageTag="$version" \
            --region "$AWS_REGION" || log_warning "이미지 스캔 시작 실패"

        log_info "이미지 스캔이 백그라운드에서 실행 중입니다."
        log_info "스캔 결과는 AWS 콘솔에서 확인할 수 있습니다."
    fi
}

# 푸시 결과 요약
show_push_summary() {
    local version="$1"
    local ecr_uri="$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY"

    echo
    echo "=== ECR 푸시 결과 요약 ==="
    echo "리포지토리: $ECR_REPOSITORY"
    echo "리전: $AWS_REGION"
    echo "버전: $version"
    echo "이미지 URI: $ecr_uri:$version"

    if [[ "${TAG_LATEST:-false}" == "true" ]]; then
        echo "Latest URI: $ecr_uri:latest"
    fi

    if [[ "${DRY_RUN:-false}" != "true" ]]; then
        echo "ECR 콘솔: https://$AWS_REGION.console.aws.amazon.com/ecr/repositories/private/$AWS_ACCOUNT_ID/$ECR_REPOSITORY"
    fi

    echo "Git 태그: v$version"
    echo "빌드 시간: $(date)"
}

# ============================================================================
# 메인 함수
# ============================================================================
main() {
    # 명령행 인자 파싱
    while [[ $# -gt 0 ]]; do
        case $1 in
            -r|--region)
                AWS_REGION="$2"
                shift 2
                ;;
            -a|--account-id)
                AWS_ACCOUNT_ID="$2"
                shift 2
                ;;
            -e|--ecr-repo)
                ECR_REPOSITORY="$2"
                shift 2
                ;;
            -v|--version)
                SEMANTIC_VERSION="$2"
                shift 2
                ;;
            -i|--increment)
                AUTO_INCREMENT="$2"
                shift 2
                ;;
            --platform)
                PLATFORM="$2"
                shift 2
                ;;
            --multi-arch)
                MULTI_ARCH="true"
                shift
                ;;
            --latest)
                TAG_LATEST="true"
                shift
                ;;
            --scan)
                RUN_SCAN="true"
                shift
                ;;
            --force)
                FORCE_PUSH="true"
                shift
                ;;
            --dry-run)
                DRY_RUN="true"
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

    log_info "GamePulse ECR 푸시 시작"

    # 사전 검사
    check_aws_cli
    check_aws_credentials
    check_ecr_repository

    # ECR 로그인
    if [[ "${DRY_RUN:-false}" != "true" ]]; then
        ecr_login
    fi

    # 버전 결정
    local current_version=$(read_current_version)
    local new_version

    if [[ -n "$SEMANTIC_VERSION" ]]; then
        new_version="$SEMANTIC_VERSION"
        log_info "지정된 버전 사용: $new_version"
    else
        new_version=$(increment_version "$current_version" "$AUTO_INCREMENT")
        log_info "자동 증가된 버전: $current_version -> $new_version ($AUTO_INCREMENT)"
    fi

    # 태그 중복 확인
    if [[ "${FORCE_PUSH:-false}" != "true" ]] && check_image_tag_exists "$new_version"; then
        log_error "이미지 태그가 이미 존재합니다: $new_version"
        log_info "--force 옵션을 사용하여 덮어쓸 수 있습니다."
        exit 1
    fi

    # 빌드 시작 시간 기록
    local start_time=$(date +%s)

    # Docker 이미지 빌드
    build_image "$new_version"

    # 이미지 태깅
    tag_image "$new_version"

    # 이미지 푸시
    push_image "$new_version"

    # 버전 파일 저장
    if [[ "${DRY_RUN:-false}" != "true" ]]; then
        save_version "$new_version"
    fi

    # Git 태그 생성
    if [[ "${DRY_RUN:-false}" != "true" ]]; then
        create_git_tag "$new_version"
    fi

    # ECR 이미지 스캔
    if [[ "${DRY_RUN:-false}" != "true" ]]; then
        run_ecr_scan "$new_version"
    fi

    # 완료 시간 계산
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))

    log_success "ECR 푸시 완료! (소요 시간: ${duration}초)"

    # 결과 요약
    show_push_summary "$new_version"
}

# 스크립트 실행
main "$@"
