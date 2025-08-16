# Task 3: ECS 클러스터 및 ALB 인프라 구성 구현

## 개요

GamePulse AWS 배포를 위한 ECS 클러스터 및 Application Load Balancer 인프라를 Terraform으로 구성했습니다. 이 작업은 Fargate 기반 ECS 클러스터, ALB, 그리고 EFS/S3 스토리지 시스템을 포함합니다.

## 구현된 컴포넌트

### 3.1 ECS 클러스터 모듈 (✅ 완료)

**위치**: `modules/ecs/main.tf`

#### 주요 기능
- **Fargate 기반 ECS 클러스터**: 서버리스 컨테이너 실행 환경
- **Container Insights 활성화**: 클러스터 모니터링 및 메트릭 수집
- **CloudWatch 로깅**: 각 서비스별 로그 그룹 생성
- **용량 공급자**: FARGATE 및 FARGATE_SPOT 지원

#### 생성된 리소스
```terraform
# ECS 클러스터
resource "aws_ecs_cluster" "main"

# CloudWatch 로그 그룹들
- /aws/ecs/cluster/${cluster_name}
- /aws/ecs/${cluster_name}/gamepulse-app
- /aws/ecs/${cluster_name}/otel-collector
- /aws/ecs/${cluster_name}/prometheus
- /aws/ecs/${cluster_name}/loki
- /aws/ecs/${cluster_name}/jaeger
- /aws/ecs/${cluster_name}/grafana

# 용량 공급자 설정
resource "aws_ecs_cluster_capacity_providers" "main"
```

#### 설정 옵션
- **로그 보존 기간**: 환경별 설정 (staging: 14일, prod: 90일)
- **Container Insights**: 활성화
- **Execute Command**: 환경별 설정 (staging: 활성화, prod: 비활성화)

### 3.2 Application Load Balancer 모듈 (✅ 완료)

**위치**: `modules/alb/main.tf`

#### 주요 기능
- **Application Load Balancer**: 인터넷 대면 로드 밸런서
- **SSL/TLS 종료**: ACM 인증서를 통한 HTTPS 지원
- **타겟 그룹**: GamePulse 애플리케이션 및 모니터링 서비스용
- **보안 그룹**: 적절한 포트 및 프로토콜 제한

#### 생성된 리소스
```terraform
# ALB 및 관련 리소스
resource "aws_lb" "main"
resource "aws_security_group" "alb"
resource "aws_acm_certificate" "main"

# 타겟 그룹
resource "aws_lb_target_group" "app"
resource "aws_lb_target_group" "monitoring"

# 리스너
resource "aws_lb_listener" "http"
resource "aws_lb_listener" "https"
resource "aws_lb_listener" "http_only"
```

#### 보안 설정
- **인바운드**: HTTP(80), HTTPS(443) 전체 인터넷 허용
- **아웃바운드**: ECS 컨테이너(8080)로만 제한
- **SSL 정책**: ELBSecurityPolicy-TLS-1-2-2017-01

#### 헬스 체크 설정
```terraform
health_check {
  enabled             = true
  healthy_threshold   = 2
  interval            = 30
  matcher             = "200"
  path                = "/health"
  protocol            = "HTTP"
  timeout             = 5
  unhealthy_threshold = 2-3 (환경별)
}
```

### 3.3 EFS 및 S3 스토리지 모듈 (✅ 완료)

#### EFS 모듈 (`modules/efs/main.tf`)

**주요 기능**:
- **암호화된 EFS 파일 시스템**: 전송 중 및 저장 시 암호화
- **다중 AZ 마운트 타겟**: 고가용성 보장
- **서비스별 액세스 포인트**: Prometheus, Jaeger, Grafana용

**생성된 리소스**:
```terraform
# EFS 파일 시스템
resource "aws_efs_file_system" "main"

# 마운트 타겟 (각 프라이빗 서브넷)
resource "aws_efs_mount_target" "main"

# 액세스 포인트
resource "aws_efs_access_point" "prometheus"  # UID/GID: 65534
resource "aws_efs_access_point" "jaeger"      # UID/GID: 10001
resource "aws_efs_access_point" "grafana"     # UID/GID: 472
```

**라이프사이클 정책**:
- **IA 전환**: 30일 후 Infrequent Access로 전환
- **기본 스토리지 복원**: 1회 액세스 후 Standard로 복원

#### S3 모듈 (`modules/s3/main.tf`)

**주요 기능**:
- **Loki 로그 스토리지**: 장기 로그 보관용 S3 버킷
- **백업 스토리지**: 시스템 백업용 S3 버킷
- **라이프사이클 정책**: 비용 최적화를 위한 스토리지 클래스 전환

**생성된 리소스**:
```terraform
# S3 버킷
resource "aws_s3_bucket" "loki_logs"
resource "aws_s3_bucket" "backups"

# 보안 설정
resource "aws_s3_bucket_public_access_block"
resource "aws_s3_bucket_server_side_encryption_configuration"
resource "aws_s3_bucket_versioning"
```

**라이프사이클 정책**:

| 스토리지 클래스 | Loki 로그 (Prod) | 백업 (Prod) | Loki 로그 (Staging) | 백업 (Staging) |
|----------------|------------------|-------------|-------------------|---------------|
| Standard-IA    | 30일             | 30일        | 30일              | 30일          |
| Glacier        | 90일             | 90일        | 90일              | 90일          |
| Deep Archive   | 365일            | 180일       | 180일             | 180일         |
| 만료           | 2555일 (7년)     | 3650일 (10년)| 365일 (1년)       | 730일 (2년)   |

## 환경별 구성

### 프로덕션 환경 (`environments/prod.tfvars`)

```terraform
# 리소스 할당
app_cpu           = 2048
app_memory        = 4096
app_desired_count = 2

# 보안 설정
ecr_image_tag_mutability   = "IMMUTABLE"
ecs_enable_execute_command = false
enable_deletion_protection = false  # 실제 운영시 true 권장

# 로그 보존
ecs_log_retention_days = 90
```

### 스테이징 환경 (`environments/staging.tfvars`)

```terraform
# 리소스 할당 (축소)
app_cpu           = 1024
app_memory        = 2048
app_desired_count = 2

# 개발 친화적 설정
ecr_image_tag_mutability   = "MUTABLE"
ecs_enable_execute_command = true
ecr_force_delete          = true

# 로그 보존 (단기)
ecs_log_retention_days = 14
```

## 네트워킹 아키텍처

```
Internet Gateway
       |
   Public Subnets (3 AZs)
       |
   ALB (Internet-facing)
       |
   Private Subnets (3 AZs)
       |
   ECS Tasks (Fargate)
       |
   EFS Mount Targets
```

### 보안 그룹 규칙

1. **ALB 보안 그룹**:
   - 인바운드: 80, 443 (0.0.0.0/0)
   - 아웃바운드: 8080 (ECS 보안 그룹)

2. **ECS 보안 그룹**:
   - 인바운드: 8080 (ALB 보안 그룹)
   - 아웃바운드: 443 (인터넷), 2049 (EFS)

3. **EFS 보안 그룹**:
   - 인바운드: 2049 (ECS 보안 그룹)

## 출력값 (Outputs)

주요 출력값들이 `outputs.tf`에 정의되어 있어 다른 모듈이나 시스템에서 참조할 수 있습니다:

```terraform
# ECS 정보
output "ecs_cluster_arn"
output "ecs_cluster_name"
output "ecs_log_groups"

# ALB 정보
output "alb_dns_name"
output "target_group_arn"
output "ssl_certificate_arn"

# 스토리지 정보
output "efs_id"
output "loki_logs_bucket_id"
output "storage_configuration"
```

## 다음 단계

이제 ECS 클러스터, ALB, 스토리지 인프라가 준비되었으므로 다음 작업들을 진행할 수 있습니다:

1. **Task 4**: OpenTelemetry Collector 구성
2. **Task 5**: 모니터링 스택 ECS 태스크 정의 생성
3. **Task 6**: GamePulse 애플리케이션 ECS 서비스 구성

## 검증 방법

### Terraform 검증
```bash
# 구성 검증
terraform validate

# 계획 확인
terraform plan -var-file=environments/staging.tfvars

# 적용 (스테이징 환경)
terraform apply -var-file=environments/staging.tfvars
```

### 리소스 확인
```bash
# ECS 클러스터 확인
aws ecs describe-clusters --clusters gamepulse-staging

# ALB 확인
aws elbv2 describe-load-balancers --names gamepulse-alb

# EFS 확인
aws efs describe-file-systems

# S3 버킷 확인
aws s3 ls | grep gamepulse
```

## 보안 고려사항

1. **네트워크 격리**: 모든 애플리케이션 컨테이너는 프라이빗 서브넷에 배치
2. **암호화**: EFS 및 S3 모두 저장 시 암호화 활성화
3. **최소 권한**: 보안 그룹은 필요한 포트만 개방
4. **SSL/TLS**: ALB에서 SSL 종료 및 HTTPS 리다이렉트
5. **로그 보안**: CloudWatch 로그 그룹 암호화 활성화

## 비용 최적화

1. **Fargate Spot**: 비용 절약을 위한 Spot 인스턴스 지원
2. **S3 라이프사이클**: 자동 스토리지 클래스 전환으로 비용 절감
3. **EFS IA**: 자주 액세스하지 않는 파일의 자동 IA 전환
4. **로그 보존**: 환경별 적절한 로그 보존 기간 설정

이로써 Task 3 "ECS 클러스터 및 ALB 인프라 구성"이 완료되었습니다.