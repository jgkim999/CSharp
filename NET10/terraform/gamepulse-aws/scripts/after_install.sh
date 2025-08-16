#!/bin/bash

# CodeDeploy After Install Hook
# 새로운 애플리케이션 버전 설치 후 실행되는 스크립트

set -euo pipefail

echo "=== After Install Hook 시작 ==="

# 로그 설정
LOG_FILE="/tmp/codedeploy-after-install.log"
exec > >(tee -a $LOG_FILE)
exec 2>&1

echo "$(date): After Install 단계 시작"

# 새로운 태스크 정의 확인
echo "새로운 태스크 정의 확인 중..."
TASK_DEF_ARN=$(aws ecs describe-services --cluster gamepulse-cluster --services gamepulse-service --query 'services[0].taskDefinition' --output text --region ap-northeast-2)
echo "현재 태스크 정의: $TASK_DEF_ARN"

# 태스크 정의 세부 정보 확인
aws ecs describe-task-definition --task-definition $TASK_DEF_ARN --region ap-northeast-2

# 새로운 태스크가 시작되었는지 확인
echo "새로운 태스크 시작 확인 중..."
sleep 30

RUNNING_TASKS=$(aws ecs list-tasks --cluster gamepulse-cluster --service-name gamepulse-service --desired-status RUNNING --region ap-northeast-2 --query 'taskArns' --output text)
echo "실행 중인 태스크: $RUNNING_TASKS"

# 태스크 상태 확인
for task in $RUNNING_TASKS; do
    echo "태스크 $task 상태 확인 중..."
    aws ecs describe-tasks --cluster gamepulse-cluster --tasks $task --region ap-northeast-2
done

echo "$(date): After Install 단계 완료"
echo "=== After Install Hook 완료 ==="