# Demo.Web 문서 모음

이 디렉터리는 Demo.Web 프로젝트의 주요 기능에 대한 문서를 포함합니다.

## 📚 문서 카테고리

### 🚦 Rate Limiting 문서

Rate Limiting 기능의 구현, 운영, 사용에 대한 포괄적인 가이드입니다.

#### 📖 주요 가이드
- **[Rate Limiting 구현 가이드](Rate-Limiting-Implementation-Guide.md)** - 전체 구현 내용과 설정 방법
- **[Rate Limiting 운영 가이드](Rate-Limiting-Operational-Guide.md)** - 운영 환경 배포 및 모니터링
- **[Rate Limiting 빠른 참조](Rate-Limiting-Quick-Reference.md)** - 개발자용 빠른 참조 가이드

#### 🔧 구현 문서
- [Task 1 구현](Rate-Limiting-Task1-Implementation.md) - 기본 Rate Limiting 적용
- [Task 2 구현](Rate-Limiting-Task2-Implementation.md) - 사용자 정의 응답
- [Task 3 구현](Rate-Limiting-Task3-Implementation.md) - 설정 클래스 생성
- [Task 4 구현](Rate-Limiting-Task4-Implementation.md) - 로깅 구현
- [Task 7 구현](Rate-Limiting-Task7-Implementation.md) - 성능 및 부하 테스트

### 📊 OpenTelemetry 문서

OpenTelemetry 기능의 구현과 운영에 대한 문서입니다.

#### 📖 주요 가이드
- **[OpenTelemetry 구현 가이드](OpenTelemetry-Implementation-Guide.md)** - 전체 구현 가이드
- **[OpenTelemetry 운영 런북](OpenTelemetry-Operational-Runbook.md)** - 운영 가이드
- **[OpenTelemetry 성능 튜닝](OpenTelemetry-Performance-Tuning-Guide.md)** - 성능 최적화
- **[OpenTelemetry 문제 해결](OpenTelemetry-Troubleshooting-Guide.md)** - 트러블슈팅

#### 🔧 구현 문서
- [Task 3 구현](OpenTelemetry-Task3-Implementation.md)
- [Task 4.2 구현](OpenTelemetry-Task4.2-Implementation.md)
- [Task 7 구현](OpenTelemetry-Task7-Implementation.md)
- [Task 8 구현](OpenTelemetry-Task8-Implementation.md)
- [Task 9 구현](OpenTelemetry-Task9-Implementation.md)

## 🚀 빠른 시작

### Rate Limiting 사용하기
```csharp
// 엔드포인트에 Rate Limiting 적용
public override void Configure()
{
    Post("/api/your-endpoint");
    Throttle(hitLimit: 10, durationSeconds: 60);
}
```

### OpenTelemetry 사용하기
```csharp
// 커스텀 액티비티 생성
using var activity = ActivitySource.StartActivity("CustomOperation");
activity?.SetTag("operation.type", "user_creation");
```

## 📋 문서 사용 가이드

### 개발자용
1. **빠른 참조 가이드**부터 시작
2. 구체적인 구현이 필요하면 **구현 가이드** 참조
3. 문제 발생시 **문제 해결 가이드** 확인

### 운영자용
1. **운영 가이드**로 배포 및 모니터링 설정
2. **성능 튜닝 가이드**로 최적화
3. 장애 발생시 **운영 런북** 활용

### 새로운 팀원용
1. **구현 가이드**로 전체 아키텍처 이해
2. **빠른 참조 가이드**로 일상 업무 지원
3. 각 Task 구현 문서로 세부 구현 학습

## 🔄 문서 업데이트

### 업데이트 원칙
- 기능 변경시 관련 문서 동시 업데이트
- 새로운 기능 추가시 해당 가이드 문서 작성
- 문제 해결 사례는 문제 해결 가이드에 추가

### 문서 리뷰
- 월 1회 문서 정확성 검토
- 분기 1회 문서 구조 개선
- 연 1회 전체 문서 아키텍처 검토

## 📞 지원 및 피드백

### 문서 관련 문의
- **개발팀**: dev-team@company.com
- **문서 관리자**: docs-admin@company.com

### 개선 제안
- GitHub Issues를 통한 문서 개선 제안
- 팀 회의에서 문서 관련 피드백 수집

---

💡 **팁**: 각 문서는 독립적으로 읽을 수 있도록 작성되었지만, 전체적인 이해를 위해서는 구현 가이드부터 읽는 것을 권장합니다.