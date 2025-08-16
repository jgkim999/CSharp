# ECS 클러스터 Terraform 모듈

이 모듈은 GamePulse 애플리케이션을 위한 AWS ECS Fargate 클러스터를 생성합니다.

## 기능

- **Fargate 기반 ECS 클러스터**: 서버리스 컨테이너 실행 환경
- **Container Insights 활성화**: 컨테이너 메트릭 및 로그 모니터링
- **CloudWatch 로깅**: 클러스터 및 서비스별 로그 그룹 생성
- **용량 공급자**: FARGATE 및 FARGATE_SPOT 지원
- **ECS Exec 지원**: 컨테이너 디버깅을 위한 명령 실행 기능

## 사용법

```hcl
module "ecs_cluster" {
  source = "./modules/ecs"

  cluster_name         = "gamepulse-cluster"
  log_retention_days   = 30
  
  tags = {
    Environment = "production"
    Project     = "gamepulse"
    Owner       = "devops-team"
  }
}
```

## 입력 변수

| 변수명 | 설명 | 타입 | 기본값 | 필수 |
|--------|------|------|--------|------|
| `cluster_name` | ECS 클러스터 이름 | `string` | - | ✅ |
| `log_retention_days` | CloudWatch 로그 보존 기간 (일) | `number` | `30` | ❌ |
| `tags` | 리소스에 적용할 태그 | `map(string)` | `{}` | ❌ |
| `enable_container_insights` | Container Insights 활성화 여부 | `bool` | `true` | ❌ |
| `enable_execute_command` | ECS Exec 명령 실행 활성화 여부 | `bool` | `true` | ❌ |

## 출력값

| 출력명 | 설명 |
|--------|------|
| `cluster_id` | ECS 클러스터 ID |
| `cluster_arn` | ECS 클러스터 ARN |
| `cluster_name` | ECS 클러스터 이름 |
| `log_groups` | 생성된 CloudWatch 로그 그룹들 |
| `log_group_arns` | CloudWatch 로그 그룹 ARN들 |
| `capacity_providers` | ECS 클러스터 용량 공급자 |

## 생성되는 리소스

- **ECS 클러스터**: Fargate 기반 컨테이너 실행 환경
- **CloudWatch 로그 그룹들**:
  - 클러스터 로그: `/aws/ecs/cluster/{cluster_name}`
  - GamePulse 앱 로그: `/aws/ecs/{cluster_name}/gamepulse-app`
  - OpenTelemetry Collector 로그: `/aws/ecs/{cluster_name}/otel-collector`
  - Prometheus 로그: `/aws/ecs/{cluster_name}/prometheus`
  - Loki 로그: `/aws/ecs/{cluster_name}/loki`
  - Jaeger 로그: `/aws/ecs/{cluster_name}/jaeger`
  - Grafana 로그: `/aws/ecs/{cluster_name}/grafana`

## 요구사항

- AWS Provider 버전 >= 4.0
- Terraform 버전 >= 1.0

## 보안 고려사항

- Container Insights는 CloudWatch 메트릭 및 로그를 수집하므로 추가 비용이 발생할 수 있습니다
- ECS Exec 기능은 디버깅 목적으로만 사용하고 프로덕션에서는 신중하게 사용하세요
- 로그 보존 기간을 적절히 설정하여 스토리지 비용을 관리하세요

## 예제

### 기본 사용법
```hcl
module "ecs_cluster" {
  source = "./modules/ecs"

  cluster_name = "gamepulse-prod"
}
```

### 고급 설정
```hcl
module "ecs_cluster" {
  source = "./modules/ecs"

  cluster_name               = "gamepulse-prod"
  log_retention_days         = 90
  enable_container_insights  = true
  enable_execute_command     = false

  tags = {
    Environment = "production"
    Project     = "gamepulse"
    Owner       = "devops-team"
    CostCenter  = "engineering"
  }
}
```