#!/bin/bash

# OpenTelemetry Collector 구성 파일 유효성 검증 스크립트

set -e

echo "🔍 OpenTelemetry Collector 구성 파일 유효성 검증 시작..."

# 색상 정의
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 함수: 성공 메시지
success() {
    echo -e "${GREEN}✅ $1${NC}"
}

# 함수: 경고 메시지
warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

# 함수: 에러 메시지
error() {
    echo -e "${RED}❌ $1${NC}"
}

# 함수: 구성 파일 검증
validate_config() {
    local config_file=$1
    local config_name=$2
    
    echo "📋 $config_name 검증 중..."
    
    if [ ! -f "$config_file" ]; then
        error "$config_file 파일이 존재하지 않습니다."
        return 1
    fi
    
    # YAML 문법 검증 (yq 사용)
    if command -v yq &> /dev/null; then
        if yq eval '.' "$config_file" > /dev/null 2>&1; then
            success "$config_name YAML 문법이 올바릅니다."
        else
            error "$config_name YAML 문법 오류가 있습니다."
            return 1
        fi
    else
        warning "yq가 설치되지 않아 YAML 문법 검증을 건너뜁니다."
    fi
    
    # 필수 섹션 확인
    local required_sections=("receivers" "processors" "exporters" "service")
    for section in "${required_sections[@]}"; do
        if grep -q "^${section}:" "$config_file"; then
            success "$config_name에 $section 섹션이 있습니다."
        else
            error "$config_name에 $section 섹션이 없습니다."
            return 1
        fi
    done
    
    # OTLP 수신기 확인
    if grep -q "otlp:" "$config_file"; then
        success "$config_name에 OTLP 수신기가 구성되어 있습니다."
    else
        error "$config_name에 OTLP 수신기가 구성되지 않았습니다."
        return 1
    fi
    
    # 배치 프로세서 확인
    if grep -q "batch:" "$config_file"; then
        success "$config_name에 배치 프로세서가 구성되어 있습니다."
    else
        warning "$config_name에 배치 프로세서가 구성되지 않았습니다."
    fi
    
    # 메모리 제한 프로세서 확인
    if grep -q "memory_limiter:" "$config_file"; then
        success "$config_name에 메모리 제한 프로세서가 구성되어 있습니다."
    else
        warning "$config_name에 메모리 제한 프로세서가 구성되지 않았습니다."
    fi
    
    echo ""
}

# 함수: Docker 이미지 검증
validate_docker_image() {
    echo "🐳 OpenTelemetry Collector Docker 이미지 검증 중..."
    
    if docker pull otel/opentelemetry-collector-contrib:latest > /dev/null 2>&1; then
        success "OpenTelemetry Collector 이미지를 성공적으로 가져왔습니다."
    else
        error "OpenTelemetry Collector 이미지를 가져올 수 없습니다."
        return 1
    fi
    
    # 이미지 정보 출력
    echo "📊 이미지 정보:"
    docker image inspect otel/opentelemetry-collector-contrib:latest --format '{{.RepoTags}} {{.Size}} bytes' 2>/dev/null || true
    echo ""
}

# 함수: 포트 충돌 검사
check_port_conflicts() {
    echo "🔌 포트 충돌 검사 중..."
    
    local ports=(4317 4318 8888 13133)
    local conflicts=0
    
    for port in "${ports[@]}"; do
        if lsof -i :$port > /dev/null 2>&1; then
            warning "포트 $port가 이미 사용 중입니다."
            conflicts=$((conflicts + 1))
        else
            success "포트 $port는 사용 가능합니다."
        fi
    done
    
    if [ $conflicts -gt 0 ]; then
        warning "$conflicts개의 포트 충돌이 발견되었습니다."
    fi
    echo ""
}

# 메인 검증 로직
main() {
    echo "🚀 GamePulse OpenTelemetry Collector 구성 검증"
    echo "================================================"
    echo ""
    
    # 구성 파일들 검증
    validate_config "otel-collector-config.yaml" "기본 구성 파일"
    validate_config "otel-collector-docker.yaml" "Docker 구성 파일"
    validate_config "otel-collector-production.yaml" "프로덕션 구성 파일"
    validate_config "otel-collector-staging.yaml" "스테이징 구성 파일"
    
    # Docker 이미지 검증 (선택사항)
    if command -v docker &> /dev/null; then
        validate_docker_image
    else
        warning "Docker가 설치되지 않아 이미지 검증을 건너뜁니다."
    fi
    
    # 포트 충돌 검사 (선택사항)
    if command -v lsof &> /dev/null; then
        check_port_conflicts
    else
        warning "lsof가 설치되지 않아 포트 검사를 건너뜁니다."
    fi
    
    echo "🎉 구성 파일 검증이 완료되었습니다!"
    echo ""
    echo "📝 다음 단계:"
    echo "1. ECS 태스크 정의에 구성 파일 적용"
    echo "2. 모니터링 스택 (Prometheus, Loki, Jaeger) 배포"
    echo "3. GamePulse 애플리케이션에 OpenTelemetry SDK 통합"
}

# 스크립트 실행
main "$@"