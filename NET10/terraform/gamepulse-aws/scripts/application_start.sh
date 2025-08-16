#!/bin/bash

# CodeDeploy Application Start Hook
# 애플리케이션 시작 시 실행되는 스크립트

set -euo pipefail

echo "=== Application Start Hook 시작 ==="

# 로그 설정
LOG_FILE="/tmp/codedeploy-application-start.log"
exec > >(tee -a $LOG_FILE)
exec 2>&1

echo "$(date): Application Start 단계 시작"

# 애플리케이션 시작 대기
echo "애플리케이션 시작 대기 중..."
sleep 60

# 태스크 헬스 상태 확인
echo "태스크 헬스 상태 확인 중..."
RUNNING_TASKS=$(aws ecs list-tasks --cluster gamepulse-cluster --service-name gamepulse-service --desired-status RUNNING --region ap-northeast-2 --query 'taskArns' --output text)

for task in $RUNNING_TASKS; do
    echo "태스크 $task 헬스 상태 확인 중..."
    
    # 태스크 세부 정보 가져오기
    TASK_INFO=$(aws ecs describe-tasks --cluster gamepulse-cluster --tasks $task --region ap-northeast-2)
    
    # 컨테이너 상태 확인
    CONTAINER_STATUS=$(echo $TASK_INFO | jq -r '.tasks[0].containers[] | select(.name=="gamepulse-app") | .healthStatus')
    echo "컨테이너 헬스 상태: $CONTAINER_STATUS"
    
    # 헬스 상태가 HEALTHY가 될 때까지 대기
    RETRY_COUNT=0
    MAX_RETRIES=20
    
    while [[ "$CONTAINER_STATUS" != "HEALTHY" && $RETRY_COUNT -lt $MAX_RETRIES ]]; do
        echo "헬스 체크 대기 중... (시도 $((RETRY_COUNT + 1))/$MAX_RETRIES)"
        sleep 30
        
        TASK_INFO=$(aws ecs describe-tasks --cluster gamepulse-cluster --tasks $task --region ap-northeast-2)
        CONTAINER_STATUS=$(echo $TASK_INFO | jq -r '.tasks[0].containers[] | select(.name=="gamepulse-app") | .healthStatus')
        
        RETRY_COUNT=$((RETRY_COUNT + 1))
    done
    
    if [[ "$CONTAINER_STATUS" == "HEALTHY" ]]; then
        echo "태스크 $task 헬스 체크 성공"
    else
        echo "태스크 $task 헬스 체크 실패"
        exit 1
    fi
done

# ALB 타겟 그룹 헬스 체크
echo "ALB 타겟 그룹 헬스 체크 확인 중..."
TARGET_GROUP_ARN=$(aws elbv2 describe-target-groups --names gamepulse-tg --query 'TargetGroups[0].TargetGroupArn' --output text --region ap-northeast-2)

HEALTHY_TARGETS=$(aws elbv2 describe-target-health --target-group-arn $TARGET_GROUP_ARN --region ap-northeast-2 --query 'TargetHealthDescriptions[?TargetHealth.State==`healthy`]' --output json)
HEALTHY_COUNT=$(echo $HEALTHY_TARGETS | jq length)

echo "헬시한 타겟 수: $HEALTHY_COUNT"

if [[ $HEALTHY_COUNT -gt 0 ]]; then
    echo "ALB 타겟 그룹 헬스 체크 성공"
else
    echo "ALB 타겟 그룹 헬스 체크 실패"
    exit 1
fi

echo "$(date): Application Start 단계 완료"
echo "=== Application Start Hook 완료 ==="