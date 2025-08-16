#!/bin/bash

# CodeDeploy Application Stop Hook
# 애플리케이션 중지 시 실행되는 스크립트

set -euo pipefail

echo "=== Application Stop Hook 시작 ==="

# 로그 설정
LOG_FILE="/tmp/codedeploy-application-stop.log"
exec > >(tee -a $LOG_FILE)
exec 2>&1

echo "$(date): Application Stop 단계 시작"

# 현재 실행 중인 태스크 확인
echo "현재 실행 중인 태스크 확인 중..."
RUNNING_TASKS=$(aws ecs list-tasks --cluster gamepulse-cluster --service-name gamepulse-service --desired-status RUNNING --region ap-northeast-2 --query 'taskArns' --output text)

if [[ -n "$RUNNING_TASKS" ]]; then
    echo "실행 중인 태스크: $RUNNING_TASKS"
    
    # 각 태스크의 상태 로깅
    for task in $RUNNING_TASKS; do
        echo "태스크 $task 상태:"
        aws ecs describe-tasks --cluster gamepulse-cluster --tasks $task --region ap-northeast-2 --query 'tasks[0].lastStatus' --output text
    done
else
    echo "실행 중인 태스크가 없습니다."
fi

# 그레이스풀 셧다운을 위한 대기 시간
echo "그레이스풀 셧다운을 위해 30초 대기 중..."
sleep 30

# 연결 드레이닝 확인
echo "ALB 연결 드레이닝 상태 확인 중..."
TARGET_GROUP_ARN=$(aws elbv2 describe-target-groups --names gamepulse-tg --query 'TargetGroups[0].TargetGroupArn' --output text --region ap-northeast-2)

DRAINING_TARGETS=$(aws elbv2 describe-target-health --target-group-arn $TARGET_GROUP_ARN --region ap-northeast-2 --query 'TargetHealthDescriptions[?TargetHealth.State==`draining`]' --output json)
DRAINING_COUNT=$(echo $DRAINING_TARGETS | jq length)

echo "드레이닝 중인 타겟 수: $DRAINING_COUNT"

# 드레이닝이 완료될 때까지 대기
RETRY_COUNT=0
MAX_RETRIES=10

while [[ $DRAINING_COUNT -gt 0 && $RETRY_COUNT -lt $MAX_RETRIES ]]; do
    echo "연결 드레이닝 대기 중... (시도 $((RETRY_COUNT + 1))/$MAX_RETRIES)"
    sleep 30
    
    DRAINING_TARGETS=$(aws elbv2 describe-target-health --target-group-arn $TARGET_GROUP_ARN --region ap-northeast-2 --query 'TargetHealthDescriptions[?TargetHealth.State==`draining`]' --output json)
    DRAINING_COUNT=$(echo $DRAINING_TARGETS | jq length)
    
    RETRY_COUNT=$((RETRY_COUNT + 1))
done

if [[ $DRAINING_COUNT -eq 0 ]]; then
    echo "연결 드레이닝 완료"
else
    echo "연결 드레이닝 타임아웃 (일부 연결이 여전히 드레이닝 중)"
fi

echo "$(date): Application Stop 단계 완료"
echo "=== Application Stop Hook 완료 ==="