# OpenTelemetry Collector 구성 파일

이 디렉토리는 GamePulse 프로젝트의 OpenTelemetry Collector 구성 파일들을 포함합니다.

## 파일 구조

```
configs/
├── otel-collector-config.yaml          # 기본 구성 파일 (완전한 기능)
├── otel-collector-docker.yaml          # Docker 환경용 간소화된 구성
├── otel-collector-production.yaml      # 프로덕션 환경 최적화 구성
├── otel-collector-staging.yaml         # 스테이징 환경 구성
├── otel-collector-container-definition.json  # ECS 컨테이너 정의
├── Dockerfile.otel-collector           # 커스텀 이미지 빌드용
└── README.md                          # 이 파일
```

## 구성 파일 설명

### 1. otel-collector-config.yaml
- **용도**: 완전한 기능을 포함한 기본 구성 파일
- **특징**:
  - OTLP gRPC/HTTP 수신기
  - 메모리 제한 및 배치 처리
  - Prometheus, Loki, Jaeger 익스포터
  - 민감한 데이터 필터링
  - 헬스 체크 및 모니터링 확장

### 2. otel-collector-docker.yaml
- **용도**: ECS 태스크에서 사이드카로 실행될 때 사용
- **특징**:
  - 간소화된 구성
  - 환경 변수 기반 설정
  - 최소한의 리소스 사용

### 3. otel-collector-production.yaml
- **용도**: 프로덕션 환경 최적화
- **특징**:
  - 트레이스 샘플링 (10%)
  - 높은 배치 크기
  - 경고 레벨 로깅
  - 강화된 재시도 정책

### 4. otel-collector-staging.yaml
- **용도**: 스테이징 환경 테스트
- **특징**:
  - 디버그 로깅 활성화
  - 모든 데이터 수집 (샘플링 없음)
  - 로깅 익스포터 포함

## 환경 변수

구성 파일에서 사용되는 환경 변수들:

| 변수명 | 설명 | 기본값 |
|--------|------|--------|
| `ENVIRONMENT` | 배포 환경 (production/staging) | production |
| `LOKI_ENDPOINT` | Loki 서비스 엔드포인트 | loki |
| `JAEGER_ENDPOINT` | Jaeger 서비스 엔드포인트 | jaeger |
| `SERVICE_VERSION` | 서비스 버전 | latest |

## 포트 구성

| 포트 | 프로토콜 | 용도 |
|------|----------|------|
| 4317 | gRPC | OTLP gRPC 수신기 |
| 4318 | HTTP | OTLP HTTP 수신기 |
| 8888 | HTTP | 내부 메트릭 |
| 13133 | HTTP | 헬스 체크 |
| 8889 | HTTP | Prometheus 메트릭 익스포트 |

## 메모리 및 성능 설정

### 메모리 제한
- **기본 제한**: 512MB
- **스파이크 제한**: 128MB
- **Docker 환경**: 256MB/64MB

### 배치 처리
- **타임아웃**: 1-2초
- **배치 크기**: 512-2048개
- **최대 배치 크기**: 4096개

## 사용 방법

### ECS 태스크에서 사용
```json
{
  "name": "otel-collector",
  "image": "otel/opentelemetry-collector-contrib:latest",
  "command": ["--config=/etc/otelcol-contrib/otel-collector-config.yaml"]
}
```

### Docker Compose에서 사용
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

### 커스텀 이미지 빌드
```bash
docker build -f Dockerfile.otel-collector -t gamepulse/otel-collector:latest .
```

## 모니터링 및 디버깅

### 헬스 체크
```bash
curl http://localhost:13133/health
```

### 내부 메트릭 확인
```bash
curl http://localhost:8888/metrics
```

### zpages (개발 환경)
- http://localhost:55679/debug/tracez
- http://localhost:55679/debug/pipelinez

## 보안 고려사항

1. **민감한 데이터 필터링**
   - Authorization 헤더 제거
   - 사용자 정보 해싱
   - 쿠키 정보 제거

2. **네트워크 보안**
   - 내부 통신만 허용
   - TLS 설정 (프로덕션)

3. **리소스 제한**
   - 메모리 사용량 제한
   - CPU 사용량 모니터링

## 트러블슈팅

### 일반적인 문제들

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