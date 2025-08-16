#!/bin/bash
# Terraform 초기화 스크립트

set -e

echo "🚀 GamePulse AWS 인프라 Terraform 초기화 시작..."

# 현재 디렉토리 확인
if [ ! -f "main.tf" ]; then
    echo "❌ main.tf 파일을 찾을 수 없습니다. terraform/gamepulse-aws 디렉토리에서 실행해주세요."
    exit 1
fi

# Terraform 버전 확인
echo "📋 Terraform 버전 확인..."
terraform version

# Terraform 초기화
echo "🔧 Terraform 초기화 중..."
terraform init

# Terraform 구성 검증
echo "✅ Terraform 구성 검증 중..."
terraform validate

# Terraform 포맷 확인
echo "🎨 Terraform 포맷 확인 중..."
terraform fmt -check -recursive

echo "✅ Terraform 초기화 완료!"
echo ""
echo "다음 단계:"
echo "1. terraform.tfvars 파일을 생성하고 필요한 변수를 설정하세요"
echo "2. terraform plan 명령으로 실행 계획을 확인하세요"
echo "3. terraform apply 명령으로 인프라를 배포하세요"