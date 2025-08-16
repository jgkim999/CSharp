#!/bin/bash

# CodeDeploy After Allow Traffic Hook
# 트래픽 허용 후 실행되는 스크립트

set -euo pipefail

echo "=== After Allow Traffic Hook 시작 ==="

# 로그 설정
LOG_FILE="/tmp/codedeploy-after-allow-traffic.log"
exec > >(tee -a $LOG_FILE)
exec 2>&1

echo "$(date): After Allow Traffic 단계 시작"

# ALB를 통한 엔드투엔드 테스트
echo "ALB를 통한 엔드투엔드 테스트 시작..."

ALB_DNS=$(aws elbv2 describe-load-balancers --names gamepulse-alb --query 'LoadBalancers[0].DNSName' --output text --region ap-northeast-2)
echo "ALB DNS: $ALB_DNS"

# 기본 헬스 체크
echo "기본 헬스 체크 수행 중..."
for i in {1..5}; do
    if curl -f -s --max-time 10 "https://$ALB_DNS/health" > /dev/null; then
        echo "헬스 체크 성공 (시도 $i/5)"
        break
    else
        echo "헬스 체크 실패 (시도 $i/5)"
        if [[ $i -eq 5 ]]; then
            echo "헬스 체크 최종 실패"
            exit 1
        fi
        sleep 10
    fi
done

# 주요 API 엔드포인트 테스트
echo "주요 API 엔드포인트 테스트 수행 중..."

# 로그인 엔드포인트 테스트
LOGIN_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/login_response.json "https://$ALB_DNS/api/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"username":"test","password":"test"}' || echo "000")

if [[ "$LOGIN_RESPONSE" =~ ^[23][0-9][0-9]$ ]]; then
    echo "로그인 엔드포인트 테스트 성공 (HTTP $LOGIN_RESPONSE)"
else
    echo "로그인 엔드포인트 테스트 실패 (HTTP $LOGIN_RESPONSE)"
    # 로그인 실패는 경고만 출력 (테스트 계정이 없을 수 있음)
fi

# 사용자 생성 엔드포인트 테스트
USER_CREATE_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/user_create_response.json "https://$ALB_DNS/api/users" \
    -H "Content-Type: application/json" \
    -d '{"username":"testuser","email":"test@example.com","password":"testpass"}' || echo "000")

if [[ "$USER_CREATE_RESPONSE" =~ ^[23][0-9][0-9]$ ]]; then
    echo "사용자 생성 엔드포인트 테스트 성공 (HTTP $USER_CREATE_RESPONSE)"
else
    echo "사용자 생성 엔드포인트 테스트 실패 (HTTP $USER_CREATE_RESPONSE)"
    # 사용자 생성 실패는 경고만 출력 (중복 사용자일 수 있음)
fi

# 부하 테스트 (간단한 버전)
echo "간단한 부하 테스트 수행 중..."
LOAD_TEST_SUCCESS=0
LOAD_TEST_TOTAL=10

for i in $(seq 1 $LOAD_TEST_TOTAL); do
    RESPONSE_CODE=$(curl -s -w "%{http_code}" -o /dev/null --max-time 5 "https://$ALB_DNS/health")
    if [[ "$RESPONSE_CODE" == "200" ]]; then
        LOAD_TEST_SUCCESS=$((LOAD_TEST_SUCCESS + 1))
    fi
done

SUCCESS_RATE=$((LOAD_TEST_SUCCESS * 100 / LOAD_TEST_TOTAL))
echo "부하 테스트 성공률: $SUCCESS_RATE% ($LOAD_TEST_SUCCESS/$LOAD_TEST_TOTAL)"

if [[ $SUCCESS_RATE -lt 90 ]]; then
    echo "부하 테스트 성공률이 90% 미만입니다."
    exit 1
fi

# ALB 타겟 그룹 최종 상태 확인
echo "ALB 타겟 그룹 최종 상태 확인 중..."
TARGET_GROUP_ARN=$(aws elbv2 describe-target-groups --names gamepulse-tg --query 'TargetGroups[0].TargetGroupArn' --output text --region ap-northeast-2)

TARGET_HEALTH=$(aws elbv2 describe-target-health --target-group-arn $TARGET_GROUP_ARN --region ap-northeast-2)
HEALTHY_COUNT=$(echo $TARGET_HEALTH | jq '[.TargetHealthDescriptions[] | select(.TargetHealth.State=="healthy")] | length')
TOTAL_COUNT=$(echo $TARGET_HEALTH | jq '.TargetHealthDescriptions | length')

echo "헬시한 타겟: $HEALTHY_COUNT/$TOTAL_COUNT"

if [[ $HEALTHY_COUNT -eq 0 ]]; then
    echo "헬시한 타겟이 없습니다."
    exit 1
fi

# 메트릭 및 로그 확인
echo "메트릭 및 로그 수집 상태 확인 중..."

# CloudWatch 로그 그룹 확인
LOG_GROUPS=$(aws logs describe-log-groups --log-group-name-prefix "/ecs/gamepulse" --region ap-northeast-2 --query 'logGroups[].logGroupName' --output text)
echo "CloudWatch 로그 그룹: $LOG_GROUPS"

# 최근 로그 스트림 확인
for log_group in $LOG_GROUPS; do
    RECENT_STREAMS=$(aws logs describe-log-streams --log-group-name "$log_group" --order-by LastEventTime --descending --max-items 1 --region ap-northeast-2 --query 'logStreams[0].logStreamName' --output text 2>/dev/null || echo "없음")
    echo "로그 그룹 $log_group 최근 스트림: $RECENT_STREAMS"
done

# 배포 성공 메트릭 전송
echo "배포 성공 메트릭 전송 중..."
aws cloudwatch put-metric-data \
    --namespace "GamePulse/Deployment" \
    --metric-data MetricName=DeploymentSuccess,Value=1,Unit=Count \
    --region ap-northeast-2

# 배포 완료 알림 (SNS 토픽이 있는 경우)
SNS_TOPIC_ARN=$(aws sns list-topics --query 'Topics[?contains(TopicArn, `gamepulse-deployment`)].TopicArn' --output text --region ap-northeast-2 2>/dev/null || echo "")

if [[ -n "$SNS_TOPIC_ARN" ]]; then
    echo "배포 완료 알림 전송 중..."
    aws sns publish \
        --topic-arn "$SNS_TOPIC_ARN" \
        --message "GamePulse 애플리케이션 배포가 성공적으로 완료되었습니다. ALB: https://$ALB_DNS" \
        --subject "GamePulse 배포 완료" \
        --region ap-northeast-2
fi

# 배포 요약 정보 출력
echo ""
echo "=== 배포 완료 요약 ==="
echo "ALB 엔드포인트: https://$ALB_DNS"
echo "헬시한 타겟: $HEALTHY_COUNT/$TOTAL_COUNT"
echo "부하 테스트 성공률: $SUCCESS_RATE%"
echo "배포 완료 시간: $(date)"
echo "========================"

echo "$(date): After Allow Traffic 단계 완료"
echo "=== After Allow Traffic Hook 완료 ==="