#!/bin/bash

# Rate Limiting 테스트 스크립트
echo "Rate Limiting 테스트 시작..."

# 서버 URL
URL="http://localhost:5000/api/user/create"

# 테스트 데이터
DATA='{"name":"TestUser","email":"test@example.com","password":"TestPassword123!"}'

echo "10회 연속 요청을 보내서 Rate Limiting 동작 확인..."

for i in {1..12}; do
    echo "요청 #$i"
    curl -X POST "$URL" \
         -H "Content-Type: application/json" \
         -H "X-Forwarded-For: 192.168.1.100" \
         -d "$DATA" \
         -w "HTTP Status: %{http_code}, Response Time: %{time_total}s\n" \
         -s -o /dev/null
    
    # 요청 간 짧은 대기
    sleep 0.1
done

echo "테스트 완료"