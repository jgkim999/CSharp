# Task 4: OpenTelemetry Collector 구성 및 GamePulse 계측 구현

## 개요

GamePulse 애플리케이션에 OpenTelemetry 계측을 통합하고 OpenTelemetry Collector를 사이드카 컨테이너로 구성하여 메트릭, 로그, 트레이스를 수집하는 작업을 완료했습니다.

## 구현된 구성 요소

### 1. OpenTelemetry Collector 구성 파일

#### 메인 구성 파일 (`otel-collector-config.yaml`)
- **위치**: `terraform/gamepulse-aws/configs/otel-collector-config.yaml`
- **기능**:
  - OTLP gRPC/HTTP 수신기 구성 (포트 4317, 4318)
  - Prometheus 메트릭 익스포터
  - Loki 로그 익스포터  
  - Jaeger 트레이스 익스포터
  - 배치 처리 및 메모리 제한 설정
  - 리소스 속성 및 필터링 프로세서

#### Docker 환경용 구성 파일 (`otel-collector-docker.yaml`)
- **위치**: `terraform/gamepulse-aws/configs/otel-collector-docker.yaml`
- **기능**:
  - ECS 태스크 사이드카 컨테이너용 최적화된 구성
  - 컨테이너 간 네트워킹 지원
  - 리소스 제한 최적화

### 2. GamePulse 애플리케이션 OpenTelemetry 계측

#### OpenTelemetry 패키지 통합
- **위치**: `GamePulse/GamePulse.csproj`
- **추가된 패키지**:
  ```xml
  <PackageReference Include="OpenTelemetry" Version="1.12.0" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.12.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.12.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.12.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.12.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.StackExchangeRedis" Version="1.12.0-beta.2" />
  <PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.12" />
  <PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.12.0-beta.1" />
  <PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
  ```

#### Application Layer 확장 메서드
- **위치**: `Demo.Application/Extensions/OpenTelemetryApplicationExtensions.cs`
- **기능**:
  - OpenTelemetry 기본 설정 및 리소스 구성
  - 트레이싱 및 메트릭 설정
  - 환경 변수 지원
  - TelemetryService 등록

#### Infrastructure Layer 확장 메서드
- **위치**: `Demo.Infra/Extensions/OpenTelemetryInfraExtensions.cs`
- **기능**:
  - ASP.NET Core, HTTP 클라이언트, Redis 계측
  - OTLP 익스포터 구성
  - 런타임 메트릭 수집
  - GamePulseActivitySource 초기화

### 3. 사용자 정의 텔레메트리 서비스

#### TelemetryService 구현
- **위치**: `Demo.Application/Services/TelemetryService.cs`
- **기능**:
  - 사용자 정의 메트릭 생성 (카운터, 히스토그램, 게이지)
  - RTT 메트릭 전용 기록 메서드
  - Activity 관리 및 에러 처리
  - 트레이스 컨텍스트와 함께 로깅

#### GamePulseActivitySource
- **위치**: `Demo.Infra/Services/GamePulseActivitySource.cs`
- **기능**:
  - 중앙화된 ActivitySource 관리
  - 부모-자식 Activity 관계 설정
  - 분산 트레이싱 지원

### 4. 구성 및 설정

#### OpenTelemetry 구성 클래스
- **위치**: `Demo.Application/Configs/OtelConfig.cs`
- **설정 항목**:
  - OTLP 엔드포인트
  - 서비스 메타데이터 (이름, 버전, 네임스페이스)
  - 샘플링 설정
  - 익스포터 활성화 플래그
  - 배치 처리 설정

#### 애플리케이션 설정
- **위치**: `GamePulse/appsettings.json`
- **OpenTelemetry 설정**:
  ```json
  "OpenTelemetry": {
    "Endpoint": "http://192.168.0.47:4317",
    "TracesSamplerArg": "1.0",
    "ServiceName": "game-pulse",
    "ServiceVersion": "1.0.0",
    "ServiceNamespace": "production",
    "DeploymentEnvironment": "aws",
    "EnablePrometheusExporter": true,
    "MetricExportIntervalMs": 5000,
    "BatchExportTimeoutMs": 30000
  }
  ```

#### Serilog OpenTelemetry 통합
- **설정**: Serilog.Sinks.OpenTelemetry를 통한 로그 전송
- **기능**: 구조화된 로그를 OpenTelemetry Collector로 전송

### 5. 컨테이너 구성

#### GamePulse Dockerfile 최적화
- **위치**: `GamePulse/Dockerfile`
- **개선사항**:
  - 멀티 스테이지 빌드로 이미지 크기 최적화
  - 보안 강화 (non-root 사용자)
  - OpenTelemetry 환경 변수 설정
  - 헬스체크 구성

#### ECS 태스크 정의
- **위치**: `terraform/gamepulse-aws/ecs-task-definition-production.json`
- **구성**:
  - GamePulse 애플리케이션 컨테이너
  - OpenTelemetry Collector 사이드카 컨테이너
  - 컨테이너 간 의존성 설정
  - 환경 변수 및 시크릿 구성

## 데이터 플로우

### 메트릭 플로우
```
GamePulse App → OpenTelemetry SDK → OTLP (gRPC/HTTP) → OTel Collector → Prometheus
```

### 로그 플로우
```
GamePulse App → Serilog → OpenTelemetry Sink → OTLP → OTel Collector → Loki
```

### 트레이스 플로우
```
GamePulse App → OpenTelemetry SDK → OTLP → OTel Collector → Jaeger
```

## 주요 메트릭

### 애플리케이션 메트릭
- `requests_total`: HTTP 요청 수
- `request_duration_seconds`: 요청 처리 시간
- `errors_total`: 에러 발생 수
- `active_connections`: 활성 연결 수

### RTT 전용 메트릭
- `rtt_calls_total`: RTT 측정 호출 수
- `rtt_duration_seconds`: RTT 시간 분포
- `network_quality_score`: 네트워크 품질 점수
- `rtt_current_seconds`: 현재 RTT 값

### 인프라 메트릭
- ASP.NET Core 메트릭
- .NET 런타임 메트릭
- HTTP 클라이언트 메트릭
- Redis 연결 메트릭

## 환경 변수 지원

OpenTelemetry 표준 환경 변수를 지원하여 ECS 배포 시 유연한 구성이 가능합니다:

- `OTEL_EXPORTER_OTLP_ENDPOINT`: OTLP 엔드포인트
- `OTEL_SERVICE_NAME`: 서비스 이름
- `OTEL_SERVICE_VERSION`: 서비스 버전
- `OTEL_SERVICE_NAMESPACE`: 서비스 네임스페이스
- `OTEL_DEPLOYMENT_ENVIRONMENT`: 배포 환경
- `OTEL_RESOURCE_ATTRIBUTES`: 추가 리소스 속성

## 보안 고려사항

### 민감한 정보 보호
- HTTP 헤더에서 인증 정보 제거
- 사용자 정보 해시 처리
- 로그에서 개인정보 마스킹

### 네트워크 보안
- 컨테이너 간 통신은 localhost 사용
- OTLP 통신은 gRPC/HTTP 프로토콜 사용
- TLS 설정 지원 (프로덕션 환경)

## 성능 최적화

### 배치 처리
- 메트릭/로그/트레이스 배치 전송
- 메모리 사용량 제한
- 타임아웃 설정

### 샘플링
- 트레이스 샘플링 비율 조정 가능
- 로그 레벨별 필터링
- 불필요한 메트릭 제외

## 모니터링 및 헬스체크

### OpenTelemetry Collector 헬스체크
- 엔드포인트: `http://localhost:13133/health`
- ECS 태스크 헬스체크 통합

### 애플리케이션 헬스체크
- 엔드포인트: `http://localhost:8080/health`
- Prometheus 메트릭 엔드포인트: `/metrics`

## 다음 단계

이제 OpenTelemetry 계측이 완료되었으므로 다음 작업들을 진행할 수 있습니다:

1. **Task 5**: 모니터링 스택 ECS 태스크 정의 생성
2. **Task 6**: GamePulse 애플리케이션 ECS 서비스 구성
3. **Task 7**: 모니터링 스택 ECS 서비스 배포

## 검증 방법

### 로컬 테스트
```bash
# Docker Compose로 전체 스택 테스트
docker-compose up -d

# OpenTelemetry Collector 헬스체크
curl http://localhost:13133/health

# GamePulse 애플리케이션 헬스체크
curl http://localhost:8080/health

# Prometheus 메트릭 확인
curl http://localhost:8080/metrics
```

### ECS 배포 후 검증
- CloudWatch Logs에서 텔레메트리 데이터 확인
- Prometheus에서 메트릭 수집 확인
- Jaeger에서 트레이스 데이터 확인
- Grafana 대시보드에서 통합 모니터링 확인

## 요구사항 충족 확인

✅ **요구사항 3.1**: OpenTelemetry Collector 사이드카 구성 완료  
✅ **요구사항 3.2**: Prometheus 메트릭 전송 구성 완료  
✅ **요구사항 3.3**: Loki 로그 전송 구성 완료  
✅ **요구사항 3.4**: Jaeger 트레이스 전송 구성 완료  
✅ **요구사항 3.5**: 프로세서 및 익스포터 설정 완료