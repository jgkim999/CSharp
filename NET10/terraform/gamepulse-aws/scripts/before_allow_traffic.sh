#!/bin/bash

# CodeDeploy Before Allow Traffic Hook
# 트래픽 허용 전 실행되는 스크립트

set -euo pipefail

echo "=== Before Allow Traffic Hook 시작 ==="

# 로그 설정
LOG_FILE="/tmp/codedeploy-before-allow-traffic.log"
exec > >(tee -a $LOG_FILE)
exec 2>&1

echo "$(date): Before Allow Traffic 단계 시작"

# 새로운 태스크들이 모두 RUNNING 상태인지 확인
echo "새로운 태스크 상태 확인 중..."
RUNNING_TASKS=$(aws ecs list-tasks --cluster gamepulse-cluster --service-name gamepulse-service --desired-status RUNNING --region ap-northeast-2 --query 'taskArns' --output text)

if [[ -z "$RUNNING_TASKS" ]]; then
    echo "실행 중인 태스크가 없습니다."
    exit 1
fi

echo "실행 중인 태스크: $RUNNING_TASKS"

# 각 태스크의 헬스 상태 확인
ALL_HEALTHY=true

for task in $RUNNING_TASKS; do
    echo "태스크 $task 헬스 상태 확인 중..."
    
    TASK_INFO=$(aws ecs describe-tasks --cluster gamepulse-cluster --tasks $task --region ap-northeast-2)
    
    # 태스크 상태 확인
    TASK_STATUS=$(echo $TASK_INFO | jq -r '.tasks[0].lastStatus')
    echo "태스크 상태: $TASK_STATUS"
    
    if [[ "$TASK_STATUS" != "RUNNING" ]]; then
        echo "태스크가 RUNNING 상태가 아닙니다: $TASK_STATUS"
        ALL_HEALTHY=false
        continue
    fi
    
    # 컨테이너 헬스 상태 확인
    CONTAINER_HEALTH=$(echo $TASK_INFO | jq -r '.tasks[0].containers[] | select(.name=="gamepulse-app") | .healthStatus')
    echo "컨테이너 헬스 상태: $CONTAINER_HEALTH"
    
    if [[ "$CONTAINER_HEALTH" != "HEALTHY" ]]; then
        echo "컨테이너가 HEALTHY 상태가 아닙니다: $CONTAINER_HEALTH"
        ALL_HEALTHY=false
    fi
done

if [[ "$ALL_HEALTHY" != "true" ]]; then
    echo "일부 태스크가 헬시하지 않습니다."
    exit 1
fi

# 애플리케이션 레벨 헬스 체크
echo "애플리케이션 레벨 헬스 체크 수행 중..."

# 태스크의 프라이빗 IP 가져오기
for task in $RUNNING_TASKS; do
    TASK_INFO=$(aws ecs describe-tasks --cluster gamepulse-cluster --tasks $task --region ap-northeast-2)
    PRIVATE_IP=$(echo $TASK_INFO | jq -r '.tasks[0].attachments[0].details[] | select(.name=="privateIPv4Address") | .value')
    
    echo "태스크 $task 프라이빗 IP: $PRIVATE_IP"
    
    # 직접 헬스 체크 엔드포인트 호출
    if curl -f -s --max-time 10 "http://$PRIVATE_IP:8080/health" > /dev/null; then
        echo "태스크 $task 헬스 체크 성공"
    else
        echo "태스크 $task 헬스 체크 실패"
        exit 1
    fi
done

# 추가 검증: 애플리케이션 특화 헬스 체크
echo "애플리케이션 특화 헬스 체크 수행 중..."

for task in $RUNNING_TASKS; do
    TASK_INFO=$(aws ecs describe-tasks --cluster gamepulse-cluster --tasks $task --region ap-northeast-2)
    PRIVATE_IP=$(echo $TASK_INFO | jq -r '.tasks[0].attachments[0].details[] | select(.name=="privateIPv4Address") | .value')
    
    # 데이터베이스 연결 확인
    DB_CHECK=$(curl -f -s --max-time 10 "http://$PRIVATE_IP:8080/health/db" || echo "FAIL")
    if [[ "$DB_CHECK" == "FAIL" ]]; then
        echo "태스크 $task 데이터베이스 연결 실패"
        exit 1
    fi
    
    # Redis 연결 확인
    REDIS_CHECK=$(curl -f -s --max-time 10 "http://$PRIVATE_IP:8080/health/redis" || echo "FAIL")
    if [[ "$REDIS_CHECK" == "FAIL" ]]; then
        echo "태스크 $task Redis 연결 실패"
        exit 1
    fi
    
    echo "태스크 $task 모든 헬스 체크 통과"
done

# 메트릭 수집 확인
echo "OpenTelemetry 메트릭 수집 확인 중..."
for task in $RUNNING_TASKS; do
    TASK_INFO=$(aws ecs describe-tasks --cluster gamepulse-cluster --tasks $task --region ap-northeast-2)
    PRIVATE_IP=$(echo $TASK_INFO | jq -r '.tasks[0].attachments[0].details[] | select(.name=="privateIPv4Address") | .value')
    
    # OpenTelemetry Collector 메트릭 엔드포인트 확인
    OTEL_METRICS=$(curl -f -s --max-time 10 "http://$PRIVATE_IP:8888/metrics" || echo "FAIL")
    if [[ "$OTEL_METRICS" == "FAIL" ]]; then
        echo "태스크 $task OpenTelemetry Collector 메트릭 수집 실패"
        # 경고만 출력하고 계속 진행 (메트릭 수집 실패가 배포를 중단시키지 않도록)
        echo "경고: 메트릭 수집에 문제가 있지만 배포를 계속 진행합니다."
    else
        echo "태스크 $task OpenTelemetry 메트릭 수집 정상"
    fi
done

echo "$(date): Before Allow Traffic 단계 완료"
echo "=== Before Allow Traffic Hook 완료 ==="