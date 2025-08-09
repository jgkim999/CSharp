# Demo.Web OpenTelemetry 운영 가이드 (Operational Runbook)

## 개요

이 문서는 Demo.Web 프로젝트의 OpenTelemetry 모니터링 시스템을 운영하기 위한 포괄적인 가이드입니다. 모니터링 설정 절차, 알림 구성, 대시보드 설정, 문제 해결 방법을 제공합니다.

## 목차

1. [모니터링 인프라 설정](#모니터링-인프라-설정)
2. [Jaeger 트레이싱 설정](#jaeger-트레이싱-설정)
3. [Prometheus 메트릭 수집 설정](#prometheus-메트릭-수집-설정)
4. [Grafana 대시보드 구성](#grafana-대시보드-구성)
5. [알림 및 경고 설정](#알림-및-경고-설정)
6. [일상 운영 절차](#일상-운영-절차)
7. [문제 해결 가이드](#문제-해결-가이드)
8. [성능 모니터링 및 튜닝](#성능-모니터링-및-튜닝)
9. [백업 및 복구](#백업-및-복구)
10. [보안 고려사항](#보안-고려사항)

## 모니터링 인프라 설정

### 1. Docker Compose 기반 모니터링 스택 배포

#### docker-compose.monitoring.yml

```yaml
version: '3.8'

services:
  # OpenTelemetry Collector
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    container_name: otel-collector
    command: ["--config=/etc/otel-collector-config.yaml"]
    volumes:
      - ./otel-collector-config.yaml:/etc/otel-collector-config.yaml
    ports:
      - "4317:4317"   # OTLP gRPC receiver
      - "4318:4318"   # OTLP HTTP receiver
      - "8888:8888"   # Prometheus metrics
    depends_on:
      - jaeger
      - prometheus
    networks:
      - monitoring

  # Jaeger
  jaeger:
    image: jaegertracing/all-in-one:latest
    container_name: jaeger
    ports:
      - "16686:16686"  # Jaeger UI
      - "14250:14250"  # gRPC
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    networks:
      - monitoring

  # Prometheus
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - ./alert-rules.yml:/etc/prometheus/alert-rules.yml
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
      - '--storage.tsdb.retention.time=30d'
      - '--web.enable-lifecycle'
      - '--web.enable-admin-api'
    networks:
      - monitoring

  # Grafana
  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin123
      - GF_USERS_ALLOW_SIGN_UP=false
    volumes:
      - grafana_data:/var/lib/grafana
      - ./grafana/provisioning:/etc/grafana/provisioning
      - ./grafana/dashboards:/var/lib/grafana/dashboards
    networks:
      - monitoring

  # AlertManager
  alertmanager:
    image: prom/alertmanager:latest
    container_name: alertmanager
    ports:
      - "9093:9093"
    volumes:
      - ./alertmanager.yml:/etc/alertmanager/alertmanager.yml
      - alertmanager_data:/alertmanager
    networks:
      - monitoring

volumes:
  prometheus_data:
  grafana_data:
  alertmanager_data:

networks:
  monitoring:
    driver: bridge
```

#### 배포 명령어

```bash
# 모니터링 스택 시작
docker-compose -f docker-compose.monitoring.yml up -d

# 상태 확인
docker-compose -f docker-compose.monitoring.yml ps

# 로그 확인
docker-compose -f docker-compose.monitoring.yml logs -f
```

### 2. OpenTelemetry Collector 설정

#### otel-collector-config.yaml

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

  prometheus:
    config:
      scrape_configs:
        - job_name: 'demo-web'
          static_configs:
            - targets: ['host.docker.internal:5000']
          scrape_interval: 15s
          metrics_path: '/metrics'

processors:
  batch:
    timeout: 1s
    send_batch_size: 1024
    send_batch_max_size: 2048

  memory_limiter:
    limit_mib: 512
    spike_limit_mib: 128

  resource:
    attributes:
      - key: service.name
        value: demo-web
        action: upsert
      - key: service.version
        value: 1.0.0
        action: upsert

exporters:
  jaeger:
    endpoint: jaeger:14250
    tls:
      insecure: true

  prometheus:
    endpoint: "0.0.0.0:8888"
    namespace: demo_web
    const_labels:
      environment: production

  logging:
    loglevel: info

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [memory_limiter, batch, resource]
      exporters: [jaeger, logging]

    metrics:
      receivers: [otlp, prometheus]
      processors: [memory_limiter, batch, resource]
      exporters: [prometheus, logging]

  extensions: []
```

## Jaeger 트레이싱 설정

### 1. Jaeger UI 접근 및 기본 설정

#### 접근 정보
- **URL**: http://localhost:16686
- **기본 포트**: 16686
- **API 포트**: 16687

#### 기본 설정 확인 절차

1. **서비스 목록 확인**
   ```bash
   curl http://localhost:16686/api/services
   ```

2. **트레이스 검색 테스트**
   ```bash
   # 최근 1시간 트레이스 조회
   curl "http://localhost:16686/api/traces?service=demo-web&limit=20&lookback=1h"
   ```

3. **서비스 의존성 확인**
   ```bash
   curl http://localhost:16686/api/dependencies?endTs=$(date +%s)000
   ```

### 2. 트레이스 보존 정책 설정

#### Jaeger 환경 변수 설정

```yaml
# docker-compose.monitoring.yml에 추가
jaeger:
  environment:
    - SPAN_STORAGE_TYPE=elasticsearch  # 또는 cassandra
    - ES_SERVER_URLS=http://elasticsearch:9200
    - ES_INDEX_PREFIX=jaeger
    - ES_TAGS_AS_FIELDS_ALL=true
    - COLLECTOR_OTLP_ENABLED=true
    # 보존 기간 설정 (30일)
    - ES_MAX_SPAN_AGE=720h
```

### 3. 트레이스 샘플링 모니터링

#### 샘플링 비율 확인 쿼리

```bash
# 샘플링 통계 확인
curl "http://localhost:16686/api/sampling?service=demo-web"
```

## Prometheus 메트릭 수집 설정

### 1. Prometheus 구성 파일

#### prometheus.yml

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  external_labels:
    cluster: 'demo-web-cluster'
    environment: 'production'

rule_files:
  - "alert-rules.yml"

alerting:
  alertmanagers:
    - static_configs:
        - targets:
          - alertmanager:9093

scrape_configs:
  # Demo.Web 애플리케이션
  - job_name: 'demo-web'
    static_configs:
      - targets: ['host.docker.internal:5000']
    scrape_interval: 15s
    metrics_path: '/metrics'
    scrape_timeout: 10s

  # OpenTelemetry Collector
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8888']
    scrape_interval: 15s

  # Prometheus 자체 모니터링
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  # Node Exporter (시스템 메트릭)
  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']

  # Grafana 모니터링
  - job_name: 'grafana'
    static_configs:
      - targets: ['grafana:3000']
```

### 2. 메트릭 수집 확인 절차

#### 기본 확인 명령어

```bash
# Prometheus 타겟 상태 확인
curl http://localhost:9090/api/v1/targets

# 메트릭 목록 확인
curl http://localhost:9090/api/v1/label/__name__/values

# 특정 메트릭 쿼리
curl "http://localhost:9090/api/v1/query?query=demo_app_requests_total"
```

#### 주요 메트릭 검증

```bash
# HTTP 요청 메트릭 확인
curl "http://localhost:9090/api/v1/query?query=rate(demo_app_requests_total[5m])"

# 응답 시간 메트릭 확인
curl "http://localhost:9090/api/v1/query?query=histogram_quantile(0.95, rate(demo_app_request_duration_seconds_bucket[5m]))"

# 에러율 확인
curl "http://localhost:9090/api/v1/query?query=rate(demo_app_errors_total[5m])"
```

### 3. 메트릭 보존 정책

#### 보존 기간 설정

```yaml
# prometheus.yml에 추가
global:
  # 데이터 보존 기간 (30일)
  retention_time: 30d
  # 최대 저장 크기 (10GB)
  retention_size: 10GB
```

## Grafana 대시보드 구성

### 1. 데이터 소스 설정

#### Prometheus 데이터 소스 구성

```json
{
  "name": "Prometheus",
  "type": "prometheus",
  "url": "http://prometheus:9090",
  "access": "proxy",
  "isDefault": true,
  "jsonData": {
    "timeInterval": "15s",
    "queryTimeout": "60s",
    "httpMethod": "POST"
  }
}
```

#### Jaeger 데이터 소스 구성

```json
{
  "name": "Jaeger",
  "type": "jaeger",
  "url": "http://jaeger:16686",
  "access": "proxy",
  "jsonData": {
    "tracesToLogs": {
      "datasourceUid": "loki",
      "tags": ["traceID"]
    }
  }
}
```

### 2. 주요 대시보드 구성

#### Demo.Web 애플리케이션 대시보드

```json
{
  "dashboard": {
    "id": null,
    "title": "Demo.Web OpenTelemetry Dashboard",
    "tags": ["demo-web", "opentelemetry"],
    "timezone": "browser",
    "panels": [
      {
        "id": 1,
        "title": "HTTP Request Rate",
        "type": "stat",
        "targets": [
          {
            "expr": "sum(rate(demo_app_requests_total[5m]))",
            "legendFormat": "Requests/sec"
          }
        ],
        "fieldConfig": {
          "defaults": {
            "unit": "reqps",
            "color": {
              "mode": "thresholds"
            },
            "thresholds": {
              "steps": [
                {"color": "green", "value": null},
                {"color": "yellow", "value": 100},
                {"color": "red", "value": 500}
              ]
            }
          }
        }
      },
      {
        "id": 2,
        "title": "Response Time (95th percentile)",
        "type": "stat",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, sum(rate(demo_app_request_duration_seconds_bucket[5m])) by (le))",
            "legendFormat": "95th percentile"
          }
        ],
        "fieldConfig": {
          "defaults": {
            "unit": "s",
            "thresholds": {
              "steps": [
                {"color": "green", "value": null},
                {"color": "yellow", "value": 1},
                {"color": "red", "value": 3}
              ]
            }
          }
        }
      }
    ],
    "time": {
      "from": "now-1h",
      "to": "now"
    },
    "refresh": "30s"
  }
}
```

## 알림 및 경고 설정

### 1. Prometheus 알림 규칙

#### alert-rules.yml

```yaml
groups:
  - name: demo-web-alerts
    rules:
      # 높은 에러율 알림
      - alert: HighErrorRate
        expr: |
          (
            sum(rate(demo_app_errors_total[5m])) /
            sum(rate(demo_app_requests_total[5m]))
          ) * 100 > 5
        for: 2m
        labels:
          severity: warning
          service: demo-web
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value | humanizePercentage }} for the last 5 minutes"
          runbook_url: "https://wiki.company.com/runbooks/demo-web-high-error-rate"

      # 높은 응답 시간 알림
      - alert: HighResponseTime
        expr: |
          histogram_quantile(0.95, 
            sum(rate(demo_app_request_duration_seconds_bucket[5m])) by (le)
          ) > 2
        for: 5m
        labels:
          severity: warning
          service: demo-web
        annotations:
          summary: "High response time detected"
          description: "95th percentile response time is {{ $value }}s"
          runbook_url: "https://wiki.company.com/runbooks/demo-web-high-response-time"

      # 서비스 다운 알림
      - alert: ServiceDown
        expr: up{job="demo-web"} == 0
        for: 1m
        labels:
          severity: critical
          service: demo-web
        annotations:
          summary: "Demo.Web service is down"
          description: "Demo.Web service has been down for more than 1 minute"
          runbook_url: "https://wiki.company.com/runbooks/demo-web-service-down"
```

### 2. AlertManager 구성

#### alertmanager.yml

```yaml
global:
  smtp_smarthost: 'smtp.company.com:587'
  smtp_from: 'alerts@company.com'
  smtp_auth_username: 'alerts@company.com'
  smtp_auth_password: 'password'

route:
  group_by: ['alertname', 'service']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 1h
  receiver: 'web.hook'
  routes:
    - match:
        severity: critical
      receiver: 'critical-alerts'
      group_wait: 5s
      repeat_interval: 30m
    - match:
        severity: warning
      receiver: 'warning-alerts'
      repeat_interval: 2h

receivers:
  - name: 'web.hook'
    webhook_configs:
      - url: 'http://localhost:5001/webhook'

  - name: 'critical-alerts'
    email_configs:
      - to: 'oncall@company.com'
        subject: '[CRITICAL] {{ .GroupLabels.service }} Alert'
        body: |
          {{ range .Alerts }}
          Alert: {{ .Annotations.summary }}
          Description: {{ .Annotations.description }}
          Service: {{ .Labels.service }}
          Severity: {{ .Labels.severity }}
          Runbook: {{ .Annotations.runbook_url }}
          {{ end }}
    slack_configs:
      - api_url: 'https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK'
        channel: '#alerts-critical'
        title: '[CRITICAL] {{ .GroupLabels.service }} Alert'
        text: |
          {{ range .Alerts }}
          *Alert:* {{ .Annotations.summary }}
          *Description:* {{ .Annotations.description }}
          *Service:* {{ .Labels.service }}
          *Runbook:* {{ .Annotations.runbook_url }}
          {{ end }}

  - name: 'warning-alerts'
    email_configs:
      - to: 'team@company.com'
        subject: '[WARNING] {{ .GroupLabels.service }} Alert'
        body: |
          {{ range .Alerts }}
          Alert: {{ .Annotations.summary }}
          Description: {{ .Annotations.description }}
          Service: {{ .Labels.service }}
          {{ end }}
```

## 일상 운영 절차

### 1. 일일 점검 체크리스트

#### 시스템 상태 확인

```bash
#!/bin/bash
# daily-health-check.sh

echo "=== Demo.Web OpenTelemetry 일일 점검 ==="
echo "점검 시간: $(date)"
echo

# 1. 서비스 상태 확인
echo "1. 서비스 상태 확인"
docker-compose -f docker-compose.monitoring.yml ps

# 2. 메트릭 수집 상태 확인
echo -e "\n2. Prometheus 타겟 상태"
curl -s http://localhost:9090/api/v1/targets | jq '.data.activeTargets[] | {job: .labels.job, health: .health, lastError: .lastError}'

# 3. 알림 상태 확인
echo -e "\n3. 활성 알림 확인"
curl -s http://localhost:9093/api/v1/alerts | jq '.data[] | {alertname: .labels.alertname, state: .state, activeAt: .activeAt}'

# 4. 디스크 사용량 확인
echo -e "\n4. 디스크 사용량"
df -h | grep -E "(prometheus|grafana|jaeger)"

# 5. 메모리 사용량 확인
echo -e "\n5. 컨테이너 메모리 사용량"
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"

echo -e "\n=== 점검 완료 ==="
```

## 문제 해결 가이드

### 1. 일반적인 문제와 해결책

#### 문제: 트레이스가 Jaeger에 나타나지 않음

**증상**:
- Jaeger UI에서 트레이스를 찾을 수 없음
- 서비스 목록에 demo-web이 표시되지 않음

**진단 절차**:
```bash
# 1. OpenTelemetry Collector 로그 확인
docker logs otel-collector | grep -i error

# 2. 애플리케이션에서 OTLP 엔드포인트 연결 확인
curl -v http://localhost:4317

# 3. Jaeger 수신기 상태 확인
curl http://localhost:16686/api/services

# 4. 샘플링 설정 확인
curl http://localhost:16686/api/sampling?service=demo-web
```

**해결 방법**:
```bash
# 1. OpenTelemetry Collector 재시작
docker-compose -f docker-compose.monitoring.yml restart otel-collector

# 2. 애플리케이션 설정 확인
# appsettings.json에서 OTLP 엔드포인트 확인
grep -A 5 "OtlpEndpoint" appsettings.json

# 3. 네트워크 연결 확인
docker network ls
docker network inspect monitoring_monitoring
```

#### 문제: 메트릭이 Prometheus에 수집되지 않음

**증상**:
- Prometheus UI에서 demo_app_* 메트릭을 찾을 수 없음
- 타겟이 DOWN 상태

**진단 절차**:
```bash
# 1. Prometheus 타겟 상태 확인
curl http://localhost:9090/api/v1/targets

# 2. 애플리케이션 메트릭 엔드포인트 확인
curl http://localhost:5000/metrics

# 3. Prometheus 설정 확인
docker exec prometheus cat /etc/prometheus/prometheus.yml
```

**해결 방법**:
```bash
# 1. Prometheus 설정 리로드
curl -X POST http://localhost:9090/-/reload

# 2. 애플리케이션 재시작
docker-compose restart demo-web

# 3. 방화벽 설정 확인
netstat -tlnp | grep :5000
```

## 성능 모니터링 및 튜닝

### 1. 성능 기준선 설정

#### 기준 성능 지표

```yaml
# performance-baselines.yml
baselines:
  response_time:
    p50: 100ms
    p95: 500ms
    p99: 1000ms
  
  throughput:
    min_rps: 100
    target_rps: 500
    max_rps: 1000
  
  error_rate:
    warning_threshold: 1%
    critical_threshold: 5%
  
  resource_usage:
    cpu_warning: 70%
    cpu_critical: 85%
    memory_warning: 80%
    memory_critical: 90%
```

## 백업 및 복구

### 1. 데이터 백업 절차

#### 자동 백업 스크립트

```bash
#!/bin/bash
# backup-monitoring-data.sh

BACKUP_DIR="/backup/monitoring/$(date +%Y%m%d)"
mkdir -p "$BACKUP_DIR"

echo "=== 모니터링 데이터 백업 시작 ==="

# 1. Prometheus 데이터 백업
echo "1. Prometheus 데이터 백업"
docker exec prometheus promtool tsdb create-blocks-from-rules \
  --start=$(date -d '7 days ago' +%s) \
  --end=$(date +%s) \
  --url=http://localhost:9090 \
  /prometheus/backup

docker cp prometheus:/prometheus/backup "$BACKUP_DIR/prometheus"

# 2. Grafana 설정 백업
echo "2. Grafana 설정 백업"
docker cp grafana:/var/lib/grafana "$BACKUP_DIR/grafana"

# 3. 백업 압축
echo "3. 백업 파일 압축"
tar -czf "$BACKUP_DIR.tar.gz" -C /backup/monitoring "$(basename $BACKUP_DIR)"

echo "=== 백업 완료: $BACKUP_DIR.tar.gz ==="
```

## 보안 고려사항

### 1. 접근 제어 설정

#### Grafana 보안 설정

```yaml
# grafana/grafana.ini
[security]
admin_user = admin
admin_password = ${GF_SECURITY_ADMIN_PASSWORD}
secret_key = ${GF_SECURITY_SECRET_KEY}
disable_gravatar = true
cookie_secure = true
cookie_samesite = strict

[auth]
disable_login_form = false
disable_signout_menu = false

[auth.anonymous]
enabled = false

[users]
allow_sign_up = false
allow_org_create = false
auto_assign_org = true
auto_assign_org_role = Viewer
```

## 결론

이 운영 가이드를 통해 Demo.Web 프로젝트의 OpenTelemetry 모니터링 시스템을 안정적으로 운영할 수 있습니다.

### 주요 운영 포인트

1. **일일 점검**: 시스템 상태, 메트릭 수집, 알림 상태 확인
2. **주간 유지보수**: 성능 분석, 데이터 정리, 백업 확인
3. **월간 검토**: 용량 계획, 보안 업데이트, 성능 튜닝
4. **분기별 검토**: 아키텍처 개선, 새로운 모니터링 요구사항 반영

### 연락처 및 에스컬레이션

- **1차 대응**: 개발팀 (dev-team@company.com)
- **2차 대응**: 인프라팀 (infra-team@company.com)
- **긴급 상황**: 온콜 엔지니어 (+82-10-1234-5678)

### 추가 리소스

- [OpenTelemetry 공식 문서](https://opentelemetry.io/docs/)
- [Prometheus 운영 가이드](https://prometheus.io/docs/prometheus/latest/operation/)
- [Grafana 관리자 가이드](https://grafana.com/docs/grafana/latest/administration/)
- [Jaeger 운영 가이드](https://www.jaegertracing.io/docs/latest/deployment/)

정기적인 교육과 문서 업데이트를 통해 운영 품질을 지속적으로 향상시키시기 바랍니다.