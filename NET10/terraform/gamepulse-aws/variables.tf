# GamePulse AWS 배포를 위한 변수 정의
# 요구사항: 8.4

variable "aws_region" {
  description = "AWS 리전"
  type        = string
  default     = "ap-northeast-2"
}

variable "environment" {
  description = "배포 환경 (dev, staging, prod)"
  type        = string
  default     = "prod"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "환경은 dev, staging, prod 중 하나여야 합니다."
  }
}

variable "project_name" {
  description = "프로젝트 이름"
  type        = string
  default     = "gamepulse"
}

# VPC 관련 변수
variable "vpc_cidr" {
  description = "VPC CIDR 블록"
  type        = string
  default     = "10.0.0.0/16"
}

variable "availability_zones" {
  description = "사용할 가용 영역 수"
  type        = number
  default     = 3
}

# ECS 관련 변수
variable "ecs_cluster_name" {
  description = "ECS 클러스터 이름"
  type        = string
  default     = "gamepulse-cluster"
}

variable "app_cpu" {
  description = "애플리케이션 컨테이너 CPU 단위"
  type        = number
  default     = 1024
}

variable "app_memory" {
  description = "애플리케이션 컨테이너 메모리 (MB)"
  type        = number
  default     = 2048
}

variable "app_desired_count" {
  description = "애플리케이션 태스크 원하는 개수"
  type        = number
  default     = 2
}

# 모니터링 관련 변수
variable "monitoring_cpu" {
  description = "모니터링 컨테이너 CPU 단위"
  type        = number
  default     = 512
}

variable "monitoring_memory" {
  description = "모니터링 컨테이너 메모리 (MB)"
  type        = number
  default     = 1024
}

# ECR 관련 변수
variable "ecr_repository_name" {
  description = "ECR 리포지토리 이름"
  type        = string
  default     = "gamepulse"
}

variable "ecr_image_tag_mutability" {
  description = "ECR 이미지 태그 변경 가능성"
  type        = string
  default     = "MUTABLE"

  validation {
    condition     = contains(["MUTABLE", "IMMUTABLE"], var.ecr_image_tag_mutability)
    error_message = "이미지 태그 변경 가능성은 MUTABLE 또는 IMMUTABLE이어야 합니다."
  }
}

variable "ecr_scan_on_push" {
  description = "ECR 이미지 푸시 시 자동 스캔 활성화"
  type        = bool
  default     = true
}

variable "ecr_encryption_type" {
  description = "ECR 암호화 타입"
  type        = string
  default     = "AES256"

  validation {
    condition     = contains(["AES256", "KMS"], var.ecr_encryption_type)
    error_message = "암호화 타입은 AES256 또는 KMS여야 합니다."
  }
}

variable "ecr_kms_key_id" {
  description = "ECR KMS 키 ID (encryption_type이 KMS인 경우)"
  type        = string
  default     = null
}

variable "ecr_max_image_count" {
  description = "ECR에서 유지할 최대 이미지 수"
  type        = number
  default     = 10

  validation {
    condition     = var.ecr_max_image_count > 0 && var.ecr_max_image_count <= 1000
    error_message = "최대 이미지 수는 1과 1000 사이여야 합니다."
  }
}

variable "ecr_untagged_image_days" {
  description = "ECR에서 태그되지 않은 이미지 보관 일수"
  type        = number
  default     = 7

  validation {
    condition     = var.ecr_untagged_image_days > 0 && var.ecr_untagged_image_days <= 365
    error_message = "태그되지 않은 이미지 보관 일수는 1과 365 사이여야 합니다."
  }
}

variable "ecr_create_otel_repository" {
  description = "OpenTelemetry Collector용 별도 ECR 리포지토리 생성 여부"
  type        = bool
  default     = true
}

variable "ecr_force_delete" {
  description = "ECR 리포지토리 강제 삭제 허용 (개발 환경용)"
  type        = bool
  default     = false
}

# ECS 클러스터 관련 변수
variable "ecs_log_retention_days" {
  description = "ECS CloudWatch 로그 보존 기간 (일)"
  type        = number
  default     = 30

  validation {
    condition = contains([
      1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365, 400, 545, 731, 1827, 3653
    ], var.ecs_log_retention_days)
    error_message = "로그 보존 기간은 유효한 CloudWatch 로그 보존 기간이어야 합니다."
  }
}

variable "ecs_enable_container_insights" {
  description = "ECS Container Insights 활성화 여부"
  type        = bool
  default     = true
}

variable "ecs_enable_execute_command" {
  description = "ECS Exec 명령 실행 활성화 여부"
  type        = bool
  default     = true
}

# 보안 관련 변수
variable "enable_deletion_protection" {
  description = "리소스 삭제 보호 활성화"
  type        = bool
  default     = true
}

variable "backup_retention_days" {
  description = "백업 보존 기간 (일)"
  type        = number
  default     = 30
}

# ALB 관련 변수
variable "domain_name" {
  description = "SSL 인증서를 위한 도메인 이름 (빈 문자열이면 SSL 비활성화)"
  type        = string
  default     = ""
}

variable "subject_alternative_names" {
  description = "SSL 인증서의 추가 도메인 이름 목록"
  type        = list(string)
  default     = []
}

variable "ssl_policy" {
  description = "ALB SSL 보안 정책"
  type        = string
  default     = "ELBSecurityPolicy-TLS-1-2-2017-01"

  validation {
    condition = contains([
      "ELBSecurityPolicy-TLS-1-2-2017-01",
      "ELBSecurityPolicy-TLS-1-2-Ext-2018-06",
      "ELBSecurityPolicy-FS-1-2-Res-2020-10",
      "ELBSecurityPolicy-TLS-1-1-2017-01"
    ], var.ssl_policy)
    error_message = "유효한 SSL 정책을 선택해야 합니다."
  }
}

variable "health_check_path" {
  description = "ALB 타겟 그룹 헬스 체크 경로"
  type        = string
  default     = "/health"
}

variable "enable_alb_access_logs" {
  description = "ALB 액세스 로그 활성화 여부"
  type        = bool
  default     = false
}

variable "alb_access_logs_bucket" {
  description = "ALB 액세스 로그를 저장할 S3 버킷 이름"
  type        = string
  default     = ""
}

variable "alb_access_logs_prefix" {
  description = "ALB 액세스 로그 S3 접두사"
  type        = string
  default     = "alb-logs"
}

variable "enable_waf" {
  description = "AWS WAF 연결 여부"
  type        = bool
  default     = false
}

variable "waf_acl_arn" {
  description = "연결할 WAF ACL ARN"
  type        = string
  default     = ""
}

variable "enable_alb_deletion_protection" {
  description = "ALB 삭제 보호 활성화 여부"
  type        = bool
  default     = false
}

variable "enable_http2" {
  description = "HTTP/2 프로토콜 활성화 여부"
  type        = bool
  default     = true
}

variable "enable_cross_zone_load_balancing" {
  description = "크로스 존 로드 밸런싱 활성화 여부"
  type        = bool
  default     = true
}

variable "alb_idle_timeout" {
  description = "ALB 유휴 타임아웃 (초)"
  type        = number
  default     = 60

  validation {
    condition     = var.alb_idle_timeout >= 1 && var.alb_idle_timeout <= 4000
    error_message = "ALB 유휴 타임아웃은 1초에서 4000초 사이여야 합니다."
  }
}

variable "drop_invalid_header_fields" {
  description = "잘못된 헤더 필드 삭제 여부"
  type        = bool
  default     = true
}

variable "enable_stickiness" {
  description = "세션 스티키니스 활성화 여부"
  type        = bool
  default     = false
}

variable "stickiness_duration" {
  description = "스티키 세션 지속 시간 (초)"
  type        = number
  default     = 86400

  validation {
    condition     = var.stickiness_duration >= 1 && var.stickiness_duration <= 604800
    error_message = "스티키 세션 지속 시간은 1초에서 604800초(7일) 사이여야 합니다."
  }
}

variable "create_monitoring_target_groups" {
  description = "모니터링 서비스용 타겟 그룹 생성 여부"
  type        = bool
  default     = false
}

variable "target_group_health_check" {
  description = "타겟 그룹 헬스 체크 설정"
  type = object({
    enabled             = bool
    healthy_threshold   = number
    interval            = number
    matcher             = string
    path                = string
    port                = string
    protocol            = string
    timeout             = number
    unhealthy_threshold = number
  })
  default = {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200"
    path                = "/health"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 2
  }

  validation {
    condition = (
      var.target_group_health_check.healthy_threshold >= 2 &&
      var.target_group_health_check.healthy_threshold <= 10 &&
      var.target_group_health_check.unhealthy_threshold >= 2 &&
      var.target_group_health_check.unhealthy_threshold <= 10 &&
      var.target_group_health_check.interval >= 5 &&
      var.target_group_health_check.interval <= 300 &&
      var.target_group_health_check.timeout >= 2 &&
      var.target_group_health_check.timeout <= 120
    )
    error_message = "헬스 체크 설정값이 유효한 범위를 벗어났습니다."
  }
}

# EFS 관련 변수
variable "efs_performance_mode" {
  description = "EFS 성능 모드 (generalPurpose 또는 maxIO)"
  type        = string
  default     = "generalPurpose"

  validation {
    condition     = contains(["generalPurpose", "maxIO"], var.efs_performance_mode)
    error_message = "EFS 성능 모드는 generalPurpose 또는 maxIO여야 합니다."
  }
}

variable "efs_throughput_mode" {
  description = "EFS 처리량 모드 (bursting 또는 provisioned)"
  type        = string
  default     = "bursting"

  validation {
    condition     = contains(["bursting", "provisioned"], var.efs_throughput_mode)
    error_message = "EFS 처리량 모드는 bursting 또는 provisioned여야 합니다."
  }
}

variable "efs_kms_key_id" {
  description = "EFS 암호화에 사용할 KMS 키 ID (선택사항)"
  type        = string
  default     = null
}

variable "efs_transition_to_ia" {
  description = "EFS IA 스토리지 클래스로 전환할 기간"
  type        = string
  default     = "AFTER_30_DAYS"

  validation {
    condition = contains([
      "AFTER_7_DAYS", "AFTER_14_DAYS", "AFTER_30_DAYS", 
      "AFTER_60_DAYS", "AFTER_90_DAYS"
    ], var.efs_transition_to_ia)
    error_message = "유효한 IA 전환 기간을 선택해야 합니다."
  }
}

variable "efs_transition_to_primary_storage_class" {
  description = "EFS 기본 스토리지 클래스로 다시 전환할 조건"
  type        = string
  default     = "AFTER_1_ACCESS"

  validation {
    condition     = contains(["AFTER_1_ACCESS"], var.efs_transition_to_primary_storage_class)
    error_message = "유효한 기본 스토리지 클래스 전환 조건을 선택해야 합니다."
  }
}

# S3 관련 변수
variable "s3_enable_versioning" {
  description = "S3 버킷 버전 관리 활성화 여부"
  type        = bool
  default     = true
}

variable "s3_kms_key_id" {
  description = "S3 암호화에 사용할 KMS 키 ID (선택사항)"
  type        = string
  default     = null
}

# Loki 로그 S3 라이프사이클 변수
variable "s3_loki_logs_ia_transition_days" {
  description = "Loki 로그를 Standard-IA로 전환할 일수"
  type        = number
  default     = 30

  validation {
    condition     = var.s3_loki_logs_ia_transition_days >= 30
    error_message = "Standard-IA 전환은 최소 30일 후에 가능합니다."
  }
}

variable "s3_loki_logs_glacier_transition_days" {
  description = "Loki 로그를 Glacier로 전환할 일수"
  type        = number
  default     = 90

  validation {
    condition     = var.s3_loki_logs_glacier_transition_days >= 90
    error_message = "Glacier 전환은 최소 90일 후에 가능합니다."
  }
}

variable "s3_loki_logs_deep_archive_transition_days" {
  description = "Loki 로그를 Deep Archive로 전환할 일수"
  type        = number
  default     = 365

  validation {
    condition     = var.s3_loki_logs_deep_archive_transition_days >= 180
    error_message = "Deep Archive 전환은 최소 180일 후에 가능합니다."
  }
}

variable "s3_loki_logs_expiration_days" {
  description = "Loki 로그 만료 일수"
  type        = number
  default     = 2555  # 약 7년
}

variable "s3_loki_logs_noncurrent_version_expiration_days" {
  description = "Loki 로그 이전 버전 만료 일수"
  type        = number
  default     = 90
}

# 백업 S3 라이프사이클 변수
variable "s3_backups_ia_transition_days" {
  description = "백업을 Standard-IA로 전환할 일수"
  type        = number
  default     = 30

  validation {
    condition     = var.s3_backups_ia_transition_days >= 30
    error_message = "Standard-IA 전환은 최소 30일 후에 가능합니다."
  }
}

variable "s3_backups_glacier_transition_days" {
  description = "백업을 Glacier로 전환할 일수"
  type        = number
  default     = 90

  validation {
    condition     = var.s3_backups_glacier_transition_days >= 90
    error_message = "Glacier 전환은 최소 90일 후에 가능합니다."
  }
}

variable "s3_backups_deep_archive_transition_days" {
  description = "백업을 Deep Archive로 전환할 일수"
  type        = number
  default     = 180

  validation {
    condition     = var.s3_backups_deep_archive_transition_days >= 180
    error_message = "Deep Archive 전환은 최소 180일 후에 가능합니다."
  }
}

variable "s3_backups_expiration_days" {
  description = "백업 만료 일수"
  type        = number
  default     = 3650  # 10년
}

variable "s3_backups_noncurrent_version_expiration_days" {
  description = "백업 이전 버전 만료 일수"
  type        = number
  default     = 365
}

