# Task 4.1: OpenTelemetry Collector 구성 파일 작성 구현

## 개요

GamePulse 프로젝트의 OpenTelemetry Collector 구성 파일을 작성하여 애플리케이션의 메트릭, 로그, 트레이스를 수집하고 Prometheus, Loki, Jaeger로 전송하는 시스템을 구현했습니다.

## 구현된 구성 요소

### 1. 기본 구성 파일 (otel-collector-config.yaml)

완전한 기능을 포함한 프로덕션 준비 구성 파일:

**주요 특징:**
- OTLP gRPC/HTTP 수신기 (포트 4317/4318)
- 메모리 제한 프로세서 (512MB 제한)
- 배치 프로세서 (성능 최적화)
- 리소스 속성 추가 프로세서
- 민감한 데이터 필터링 프로세서
- Prometheus, Loki, Jaeger 익스포터
- 헬스 체크 및 모니터링 확장

**보안 기능:**
- Authorization 헤더 제거
- 사용자 정보 해싱
- 불필요한 메트릭 필터링

### 2. Docker 환경용 구성 (otel-collector-docker.yaml)

ECS 태스크에서 사이드카로 실행될 때 사용하는 간소화된 구성:

**특징:**
- 최소한의 리소스 사용 (256MB 메모리 제한)
- 환경 변수 기반 설정
- 핵심 기능만 포함

### 3. 환경별 구성 파일

#### 프로덕션 구성 (otel-collector-production.yaml)
- 트레이스 샘플링 (10%)
- 높은 배치 크기 (2048개)
- 경고 레벨 로깅
- 강화된 재시도 정책

#### 스테이징 구성 (otel-collector-staging.yaml)
- 디버그 로깅 활성화
- 모든 데이터 수집 (샘플링 없음)
- 로깅 익스포터 포함

### 4. ECS 통합 파일들

#### 컨테이너 정의 (otel-collector-container-definition.json)
- ECS 태스크 정의용 JSON 형식
- 포트 매핑 및 환경 변수 설정
- 헬스 체크 구성
- CloudWatch 로그 설정

#### Dockerfile (Dockerfile.otel-collector)
- 커스텀 이미지 빌드용
- 구성 파일 포함
- 헬스 체크 설정

## 구성된 파이프라인

### 메트릭 파이프라인
```
OTLP 수신기 → 메모리 제한 → 리소스 속성 → 배치 처리 → Prometheus 익스포터
```

### 로그 파이프라인
```
OTLP 수신기 → 메모리 제한 → 리소스 속성 → 속성 필터링 → 배치 처리 → Loki 익스포터
```

### 트레이스 파이프라인
```
OTLP 수신기 → 메모리 제한 → 리소스 속성 → 속성 필터링 → 배치 처리 → Jaeger 익스포터
```

## 포트 구성

| 포트 | 프로토콜 | 용도 |
|------|----------|------|
| 4317 | gRPC | OTLP gRPC 수신기 |
| 4318 | HTTP | OTLP HTTP 수신기 |
| 8888 | HTTP | 내부 메트릭 |
| 13133 | HTTP | 헬스 체크 |
| 8889 | HTTP | Prometheus 메트릭 익스포트 |

## 환경 변수

| 변수명 | 설명 | 기본값 |
|--------|------|--------|
| `ENVIRONMENT` | 배포 환경 | production |
| `LOKI_ENDPOINT` | Loki 서비스 엔드포인트 | loki |
| `JAEGER_ENDPOINT` | Jaeger 서비스 엔드포인트 | jaeger |
| `SERVICE_VERSION` | 서비스 버전 | latest |

## 성능 최적화 설정

### 메모리 관리
- **기본 제한**: 512MB (프로덕션), 256MB (Docker)
- **스파이크 제한**: 128MB (프로덕션), 64MB (Docker)
- **체크 간격**: 5초

### 배치 처리
- **타임아웃**: 1-2초
- **배치 크기**: 512-2048개
- **최대 배치 크기**: 4096개

### 재시도 정책
- **초기 간격**: 5초
- **최대 간격**: 30-60초
- **최대 경과 시간**: 300초

## 보안 구현

### 데이터 필터링
```yaml
attributes:
  actions:
    - key: http.request.header.authorization
      action: delete
    - key: http.request.header.cookie
      action: delete
    - key: user.email
      action: hash
    - key: user.id
      action: hash
```

### 불필요한 데이터 제거
```yaml
filter:
  metrics:
    exclude:
      match_type: regexp
      metric_names:
        - ".*grpc_io.*"
        - ".*_bucket"
  traces:
    exclude:
      match_type: strict
      span_names:
        - "health_check"
        - "readiness_check"
```

## 모니터링 및 디버깅

### 헬스 체크
```bash
curl http://localhost:13133/health
```

### 내부 메트릭
```bash
curl http://localhost:8888/metrics
```

### zpages (개발 환경)
- http://localhost:55679/debug/tracez
- http://localhost:55679/debug/pipelinez

## 검증 도구

### 구성 검증 스크립트 (validate-config.sh)
- YAML 문법 검증
- 필수 섹션 확인
- Docker 이미지 검증
- 포트 충돌 검사

**실행 방법:**
```bash
cd terraform/gamepulse-aws/configs
./validate-config.sh
```

## 사용 방법

### ECS 태스크에서 사용
```json
{
  "name": "otel-collector",
  "image": "otel/opentelemetry-collector-contrib:latest",
  "command": ["--config=/etc/otelcol-contrib/otel-collector-config.yaml"]
}
```

### Docker Compose에서 테스트
```yaml
services:
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    command: ["--config=/etc/otelcol-contrib/otel-collector-config.yaml"]
    volumes:
      - ./configs/otel-collector-docker.yaml:/etc/otelcol-contrib/otel-collector-config.yaml
    ports:
      - "4317:4317"
      - "4318:4318"
```

## 다음 단계

1. **GamePulse 애플리케이션 계측** (Task 4.2)
   - .NET OpenTelemetry SDK 통합
   - OTLP 엔드포인트 구성
   - 커스텀 메트릭 추가

2. **모니터링 스택 배포** (Task 5.x)
   - Prometheus ECS 서비스
   - Loki ECS 서비스
   - Jaeger ECS 서비스

3. **통합 테스트** (Task 10.1)
   - 텔레메트리 데이터 플로우 검증
   - 성능 테스트
   - 알림 시스템 테스트

## 트러블슈팅

### 일반적인 문제

1. **메모리 부족**
   - `memory_limiter` 설정 확인
   - 배치 크기 조정

2. **연결 실패**
   - 엔드포인트 URL 확인
   - 네트워크 연결 상태 점검

3. **데이터 손실**
   - 재시도 정책 확인
   - 배치 타임아웃 조정

### 로그 확인
```bash
# ECS 로그 확인
aws logs get-log-events --log-group-name /ecs/gamepulse-otel-collector

# 컨테이너 로그 확인
docker logs otel-collector
```

## 요구사항 충족 확인

✅ **요구사항 3.2**: OTLP receivers 및 processors 설정 완료
✅ **요구사항 3.3**: Prometheus, Loki, Jaeger exporters 구성 완료
✅ **요구사항 3.4**: 배치 처리 및 메모리 제한 설정 완료
✅ **요구사항 3.5**: 환경별 구성 및 보안 설정 완료

## 결론

OpenTelemetry Collector 구성 파일이 성공적으로 작성되었으며, 프로덕션 환경에서 안정적으로 운영할 수 있는 모든 필수 기능이 포함되었습니다. 다음 단계인 GamePulse 애플리케이션 계측을 진행할 수 있습니다.