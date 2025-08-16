# Task 1.3: IAM 역할 및 정책 생성 구현

## 개요

GamePulse AWS 배포를 위한 IAM 역할 및 정책을 생성하여 ECS 태스크 실행 역할, 태스크 역할, 그리고 ECR, EFS, S3, Secrets Manager에 대한 접근 권한을 정의했습니다.

## 구현된 IAM 역할

### 1. ECS 태스크 실행 역할 (Task Execution Role)

**역할명**: `{project_name}-ecs-task-execution-role`

**목적**: ECS가 컨테이너를 시작하고 관리하는 데 필요한 권한 제공

**권한**:
- Amazon ECS Task Execution Role Policy (기본 정책)
- ECR 이미지 풀링 권한
- CloudWatch 로그 생성 및 쓰기 권한
- Secrets Manager에서 시크릿 값 읽기 권한

```terraform
resource "aws_iam_role" "ecs_task_execution_role" {
  name = "${var.project_name}-ecs-task-execution-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
      }
    ]
  })
}
```

### 2. ECS 태스크 역할 (Task Role)

**역할명**: `{project_name}-ecs-task-role`

**목적**: 애플리케이션 컨테이너가 AWS 서비스에 접근하는 데 필요한 권한 제공

**권한**:
- S3 버킷 읽기/쓰기/삭제 권한
- Secrets Manager 시크릿 읽기 권한
- CloudWatch 메트릭 및 로그 권한
- EFS 파일 시스템 접근 권한

### 3. 모니터링 태스크 역할 (Monitoring Task Role)

**역할명**: `{project_name}-monitoring-task-role`

**목적**: Prometheus, Loki, Jaeger, Grafana 등 모니터링 스택이 필요한 AWS 서비스에 접근

**권한**:
- CloudWatch 메트릭 읽기 권한
- ECS 클러스터 및 서비스 정보 읽기 권한
- S3 버킷 (Loki 로그 저장용) 접근 권한
- EFS 파일 시스템 접근 권한

### 4. ECS 서비스 역할 (Service Role)

**역할명**: `{project_name}-ecs-service-role`

**목적**: ECS 서비스가 로드 밸런서와 통합하는 데 필요한 권한 제공

### 5. ECS Auto Scaling 역할

**역할명**: `{project_name}-ecs-autoscaling-role`

**목적**: ECS 서비스의 자동 확장/축소 기능 제공

### 6. CodeBuild 역할

**역할명**: `{project_name}-codebuild-role`

**목적**: CI/CD 파이프라인에서 Docker 이미지 빌드 및 ECR 푸시

**권한**:
- CloudWatch 로그 생성 및 쓰기
- ECR 이미지 푸시/풀 권한
- S3 아티팩트 접근 권한

## 보안 고려사항

### 1. 최소 권한 원칙 (Principle of Least Privilege)

각 역할은 해당 기능을 수행하는 데 필요한 최소한의 권한만 부여받습니다:

- **리소스 제한**: 특정 프로젝트 리소스에만 접근 가능
- **액션 제한**: 필요한 API 호출만 허용
- **조건부 접근**: 필요시 IP 주소나 시간 기반 제한 적용 가능

### 2. 리소스 기반 권한

```terraform
Resource = [
  "arn:aws:s3:::${var.project_name}-*",
  "arn:aws:s3:::${var.project_name}-*/*"
]
```

프로젝트별로 명명된 리소스에만 접근을 제한하여 다른 프로젝트의 리소스에 실수로 접근하는 것을 방지합니다.

### 3. 시크릿 관리

```terraform
Resource = [
  "arn:aws:secretsmanager:${var.aws_region}:${var.account_id}:secret:${var.project_name}/*"
]
```

Secrets Manager의 시크릿도 프로젝트별로 네임스페이스를 분리하여 관리합니다.

## 출력값 (Outputs)

다른 Terraform 모듈에서 참조할 수 있도록 다음 출력값들을 제공합니다:

- `ecs_task_execution_role_arn`: ECS 태스크 정의에서 사용
- `ecs_task_role_arn`: 애플리케이션 컨테이너에서 사용
- `monitoring_task_role_arn`: 모니터링 스택에서 사용
- `ecs_service_role_arn`: ECS 서비스 정의에서 사용
- `ecs_autoscaling_role_arn`: Auto Scaling 정책에서 사용
- `codebuild_role_arn`: CI/CD 파이프라인에서 사용

## 사용 예시

### ECS 태스크 정의에서 역할 사용

```terraform
resource "aws_ecs_task_definition" "gamepulse_app" {
  family                   = "gamepulse-app"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.app_cpu
  memory                   = var.app_memory
  
  execution_role_arn = module.iam.ecs_task_execution_role_arn
  task_role_arn      = module.iam.ecs_task_role_arn
  
  # 컨테이너 정의...
}
```

### 모니터링 스택에서 역할 사용

```terraform
resource "aws_ecs_task_definition" "prometheus" {
  family                   = "prometheus"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  
  execution_role_arn = module.iam.ecs_task_execution_role_arn
  task_role_arn      = module.iam.monitoring_task_role_arn
  
  # 컨테이너 정의...
}
```

## 검증 방법

### 1. Terraform 계획 확인

```bash
cd terraform/gamepulse-aws
terraform plan -var-file="environments/staging.tfvars"
```

### 2. IAM 역할 생성 확인

```bash
# AWS CLI를 통한 역할 확인
aws iam get-role --role-name gamepulse-ecs-task-execution-role
aws iam get-role --role-name gamepulse-ecs-task-role
aws iam get-role --role-name gamepulse-monitoring-task-role
```

### 3. 정책 연결 확인

```bash
# 연결된 정책 확인
aws iam list-attached-role-policies --role-name gamepulse-ecs-task-execution-role
aws iam list-role-policies --role-name gamepulse-ecs-task-role
```

## 다음 단계

1. **ECR 리포지토리 생성** (Task 2.1): IAM 역할을 사용하여 ECR 리포지토리 생성
2. **ECS 클러스터 구성** (Task 3.1): 생성된 IAM 역할을 ECS 태스크 정의에 적용
3. **모니터링 스택 배포** (Task 4-8): 모니터링 태스크 역할을 사용하여 각 모니터링 컴포넌트 배포

## 참고 자료

- [AWS ECS Task Execution IAM Role](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task_execution_IAM_role.html)
- [AWS ECS Task IAM Roles](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task-iam-roles.html)
- [AWS IAM Best Practices](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html)