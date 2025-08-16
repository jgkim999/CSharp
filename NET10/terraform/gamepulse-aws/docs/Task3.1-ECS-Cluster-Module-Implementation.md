# Task 3.1: ECS 클러스터 Terraform 모듈 구현

## 개요

GamePulse AWS 배포를 위한 ECS 클러스터 Terraform 모듈을 생성했습니다. 이 모듈은 Fargate 기반 ECS 클러스터를 생성하고 Container Insights 및 클러스터 로깅을 활성화합니다.

## 구현된 기능

### 1. ECS 클러스터 생성
- **Fargate 기반**: 서버리스 컨테이너 실행 환경
- **Container Insights 활성화**: 컨테이너 메트릭 및 로그 모니터링
- **용량 공급자**: FARGATE 및 FARGATE_SPOT 지원

### 2. CloudWatch 로깅 구성
- 클러스터 로그 그룹: `/aws/ecs/cluster/{cluster_name}`
- 애플리케이션별 로그 그룹:
  - GamePulse 앱: `/aws/ecs/{cluster_name}/gamepulse-app`
  - OpenTelemetry Collector: `/aws/ecs/{cluster_name}/otel-collector`
  - Prometheus: `/aws/ecs/{cluster_name}/prometheus`
  - Loki: `/aws/ecs/{cluster_name}/loki`
  - Jaeger: `/aws/ecs/{cluster_name}/jaeger`
  - Grafana: `/aws/ecs/{cluster_name}/grafana`

### 3. ECS Exec 지원
- 컨테이너 디버깅을 위한 명령 실행 기능
- CloudWatch 로그 암호화 활성화

## 생성된 파일

### 모듈 파일
- `modules/ecs/main.tf`: 메인 리소스 정의
- `modules/ecs/variables.tf`: 입력 변수 정의
- `modules/ecs/outputs.tf`: 출력값 정의
- `modules/ecs/README.md`: 모듈 사용법 문서

### 구성 파일 업데이트
- `main.tf`: ECS 모듈 추가
- `variables.tf`: ECS 관련 변수 추가
- `outputs.tf`: ECS 관련 출력값 추가
- `environments/staging.tfvars`: 스테이징 환경 ECS 설정
- `environments/prod.tfvars`: 프로덕션 환경 ECS 설정

## 주요 리소스

### AWS ECS 클러스터
```hcl
resource "aws_ecs_cluster" "main" {
  name = var.cluster_name

  setting {
    name  = "containerInsights"
    value = "enabled"
  }

  configuration {
    execute_command_configuration {
      logging = "OVERRIDE"
      
      log_configuration {
        cloud_watch_encryption_enabled = true
        cloud_watch_log_group_name     = aws_cloudwatch_log_group.ecs_cluster.name
      }
    }
  }
}
```

### 용량 공급자 구성
```hcl
resource "aws_ecs_cluster_capacity_providers" "main" {
  cluster_name = aws_ecs_cluster.main.name

  capacity_providers = ["FARGATE", "FARGATE_SPOT"]

  default_capacity_provider_strategy {
    base              = 1
    weight            = 100
    capacity_provider = "FARGATE"
  }
}
```

## 환경별 설정

### 스테이징 환경
- 로그 보존 기간: 14일
- Container Insights: 활성화
- ECS Exec: 활성화 (디버깅용)

### 프로덕션 환경
- 로그 보존 기간: 90일
- Container Insights: 활성화
- ECS Exec: 비활성화 (보안상)

## 사용법

```hcl
module "ecs" {
  source = "./modules/ecs"

  cluster_name               = "${var.project_name}-${var.environment}"
  log_retention_days         = var.ecs_log_retention_days
  enable_container_insights  = var.ecs_enable_container_insights
  enable_execute_command     = var.ecs_enable_execute_command

  tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}
```

## 검증 결과

### Terraform 검증
```bash
$ terraform init
Initializing modules...
- ecs in modules/ecs
Terraform has been successfully initialized!

$ terraform validate
Success! The configuration is valid.

$ terraform fmt -recursive
# 모든 파일이 올바르게 포맷됨
```

## 보안 고려사항

1. **Container Insights**: CloudWatch 메트릭 수집으로 추가 비용 발생 가능
2. **ECS Exec**: 프로덕션에서는 보안상 비활성화 권장
3. **로그 암호화**: CloudWatch 로그 암호화 활성화
4. **IAM 권한**: 최소 권한 원칙 적용 필요

## 다음 단계

1. **ALB 모듈 생성**: Application Load Balancer 구성
2. **EFS/S3 스토리지 모듈**: 영구 스토리지 구성
3. **ECS 태스크 정의**: GamePulse 애플리케이션 및 모니터링 스택 태스크 정의
4. **ECS 서비스 생성**: 실제 서비스 배포 구성

## 요구사항 충족 확인

✅ **요구사항 2.1**: Fargate 기반 ECS 클러스터 생성 완료
- ECS 클러스터가 Fargate 런치 타입으로 구성됨
- 용량 공급자에 FARGATE 및 FARGATE_SPOT 설정

✅ **Container Insights 활성화**: 컨테이너 메트릭 및 로그 모니터링 지원
- `containerInsights` 설정을 `enabled`로 구성
- CloudWatch 로그 그룹 자동 생성

✅ **클러스터 로깅 구성**: ECS Exec 및 CloudWatch 로깅 활성화
- 암호화된 CloudWatch 로그 그룹 생성
- 각 서비스별 전용 로그 그룹 구성

이로써 Task 3.1 "ECS 클러스터 Terraform 모듈 생성"이 성공적으로 완료되었습니다.