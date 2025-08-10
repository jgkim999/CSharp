# Rate Limiting 빠른 참조 가이드

## 🚀 빠른 시작

### 기본 설정

```csharp
// 엔드포인트에 Rate Limiting 적용
public override void Configure()
{
    Post("/api/your-endpoint");
    Throttle(hitLimit: 10, durationSeconds: 60);
}
```

### 설정 파일

```json
{
  "RateLimit": {
    "HitLimit": 10,
    "DurationSeconds": 60
  }
}
```

## 📊 현재 구현 상태

| 기능 | 상태 | 설명 |
|------|------|------|
| 기본 Rate Limiting | ✅ | IP별 분당 10회 제한 |
| 사용자 정의 응답 | ✅ | 429 상태코드 + 에러 메시지 |
| 설정 관리 | ✅ | appsettings.json 기반 |
| 로깅 | ✅ | 위반시 경고 로그 |
| 단위 테스트 | ✅ | 핵심 기능 테스트 완료 |
| 통합 테스트 | ✅ | HTTP 요청 기반 테스트 |
| 성능 테스트 | ✅ | 부하 테스트 완료 |
| 문서화 | ✅ | 구현/운영 가이드 완료 |

## 🔧 주요 설정

### 환경별 권장 설정

| 환경 | HitLimit | DurationSeconds | 용도 |
|------|----------|-----------------|------|
| Development | 100 | 60 | 개발 편의성 |
| Staging | 50 | 60 | 테스트 환경 |
| Production | 10 | 60 | 운영 환경 |

### 클라이언트 식별 우선순위

1. `X-Forwarded-For` 헤더
2. `HttpContext.Connection.RemoteIpAddress`
3. 실패시 403 Forbidden 응답

## 🚨 중요 제한사항

### ⚠️ 보안 제한사항

- **DDOS 방어 부적합**: 강력한 공격 방어용 아님
- **헤더 조작 가능**: X-Forwarded-For 헤더 조작 가능
- **NAT 환경 이슈**: 동일 IP 공유시 부정확

### 💡 권장 대안

- API Gateway 레벨 Rate Limiting
- 인증 기반 Rate Limiting
- 다층 보안 전략

## 📝 로그 패턴

### 정상 동작

```
[INFO] Rate limit applied for IP: 192.168.1.100, Endpoint: /api/user/create
```

### 위반 발생

```
[WARN] Rate limit exceeded for IP: 192.168.1.100, Endpoint: /api/user/create, Count: 11
```

## 🔍 디버깅 명령어

### 로그 확인

```bash
# Rate Limit 관련 로그 검색
grep "Rate limit" logs/demo-web-*.log

# 특정 IP 위반 횟수
grep "Rate limit exceeded.*IP: 192.168.1.100" logs/*.log | wc -l

# 시간대별 위반 분포
grep "Rate limit exceeded" logs/*.log | awk '{print $1" "$2}' | cut -c1-13 | uniq -c
```

### 테스트 명령어

```bash
# Rate Limit 테스트
for i in {1..15}; do
    curl -X POST http://localhost:5000/api/user/create \
         -H "Content-Type: application/json" \
         -d '{"name":"test","email":"test@example.com"}' \
         -w "Request $i: %{http_code}\n"
done
```

## 🛠️ 문제 해결

### 자주 발생하는 문제

| 문제 | 원인 | 해결책 |
|------|------|--------|
| 429 응답 과다 | 임계값 너무 낮음 | HitLimit 증가 |
| Rate Limit 미작동 | 프록시 설정 오류 | X-Forwarded-For 헤더 확인 |
| 정당한 사용자 차단 | NAT 환경 | 인증 기반 제한 고려 |

### 긴급 대응

#### Rate Limit 일시 비활성화

```csharp
// 긴급시 Rate Limit 비활성화
public override void Configure()
{
    Post("/api/user/create");
    // Throttle() 주석 처리 또는 제거
}
```

#### 설정 즉시 변경

```json
{
  "RateLimit": {
    "HitLimit": 1000,  // 임시로 크게 증가
    "DurationSeconds": 60
  }
}
```

## 📞 연락처 및 리소스

### 관련 문서

- [구현 가이드](Rate-Limiting-Implementation-Guide.md)
- [운영 가이드](Rate-Limiting-Operational-Guide.md)
- [FastEndpoints 문서](https://fast-endpoints.com/)

### 지원팀

- **개발팀**: dev-team@company.com
- **운영팀**: ops-team@company.com
- **보안팀**: security-team@company.com

## 🔄 업데이트 이력

| 날짜 | 버전 | 변경사항 |
|------|------|----------|
| 2025-01-10 | 1.0.0 | 초기 Rate Limiting 구현 |

---

💡 **팁**: 이 문서는 빠른 참조용입니다. 자세한 내용은 구현 가이드와 운영 가이드를 참조하세요.