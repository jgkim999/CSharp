#!/bin/bash
# Terraform 계획 실행 스크립트

set -e

# 환경 변수 설정 (기본값: prod)
ENVIRONMENT=${1:-prod}

echo "🔍 GamePulse AWS 인프라 Terraform 계획 실행..."
echo "📋 환경: $ENVIRONMENT"

# 환경별 변수 파일 확인
VAR_FILE="environments/${ENVIRONMENT}.tfvars"
if [ ! -f "$VAR_FILE" ]; then
    echo "❌ 환경 변수 파일을 찾을 수 없습니다: $VAR_FILE"
    echo "사용 가능한 환경: dev, staging, prod"
    exit 1
fi

# terraform.tfvars 파일 확인
if [ ! -f "terraform.tfvars" ]; then
    echo "⚠️  terraform.tfvars 파일이 없습니다."
    echo "terraform.tfvars.example을 참고하여 terraform.tfvars 파일을 생성하세요."
fi

# Terraform 계획 실행
echo "📊 Terraform 계획 실행 중..."
terraform plan -var-file="$VAR_FILE" -out="terraform-${ENVIRONMENT}.plan"

echo "✅ Terraform 계획 완료!"
echo ""
echo "계획 파일이 생성되었습니다: terraform-${ENVIRONMENT}.plan"
echo "다음 명령으로 인프라를 배포할 수 있습니다:"
echo "terraform apply terraform-${ENVIRONMENT}.plan"