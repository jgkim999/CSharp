#!/bin/bash

# CodeDeploy Before Install Hook
# 새로운 애플리케이션 버전 설치 전 실행되는 스크립트

set -euo pipefail

echo "=== Before Install Hook 시작 ==="

# 로그 설정
LOG_FILE="/tmp/codedeploy-before-install.log"
exec > >(tee -a $LOG_FILE)
exec 2>&1

echo "$(date): Before Install 단계 시작"

# 환경 변수 확인
echo "현재 환경 변수:"
env | grep -E "(AWS_|DEPLOYMENT_)" || echo "관련 환경 변수 없음"

# ECS 클러스터 상태 확인
echo "ECS 클러스터 상태 확인 중..."
aws ecs describe-clusters --clusters gamepulse-cluster --region ap-northeast-2

# 현재 실행 중인 태스크 확인
echo "현재 실행 중인 태스크 확인 중..."
aws ecs list-tasks --cluster gamepulse-cluster --service-name gamepulse-service --region ap-northeast-2

# 헬스 체크 엔드포인트 확인
echo "현재 서비스 헬스 체크..."
ALB_DNS=$(aws elbv2 describe-load-balancers --names gamepulse-alb --query 'LoadBalancers[0].DNSName' --output text --region ap-northeast-2)
curl -f -s "https://$ALB_DNS/health" || echo "현재 서비스 헬스 체크 실패 (정상적인 상황일 수 있음)"

echo "$(date): Before Install 단계 완료"
echo "=== Before Install Hook 완료 ==="