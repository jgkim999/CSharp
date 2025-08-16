#!/bin/bash
# Terraform 적용 스크립트

set -e

# 환경 변수 설정 (기본값: prod)
ENVIRONMENT=${1:-prod}

echo "🚀 GamePulse AWS 인프라 Terraform 적용 시작..."
echo "📋 환경: $ENVIRONMENT"

# 계획 파일 확인
PLAN_FILE="terraform-${ENVIRONMENT}.plan"
if [ ! -f "$PLAN_FILE" ]; then
    echo "❌ 계획 파일을 찾을 수 없습니다: $PLAN_FILE"
    echo "먼저 ./scripts/plan.sh $ENVIRONMENT 명령을 실행하세요."
    exit 1
fi

# 확인 메시지
echo "⚠️  이 작업은 AWS 리소스를 생성하며 비용이 발생할 수 있습니다."
read -p "계속하시겠습니까? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ 작업이 취소되었습니다."
    exit 1
fi

# Terraform 적용
echo "🔧 Terraform 적용 중..."
terraform apply "$PLAN_FILE"

echo "✅ GamePulse AWS 인프라 배포 완료!"
echo ""
echo "배포된 리소스 정보:"
terraform output