# ECR 모듈 변수 정의

variable "repository_name" {
  description = "ECR 리포지토리 이름"
  type        = string
  default     = "gamepulse"

  validation {
    condition     = can(regex("^[a-z0-9](?:[a-z0-9._-]*[a-z0-9])?$", var.repository_name))
    error_message = "리포지토리 이름은 소문자, 숫자, 하이픈, 밑줄, 점만 포함할 수 있습니다."
  }
}

variable "image_tag_mutability" {
  description = "이미지 태그 변경 가능성 설정"
  type        = string
  default     = "MUTABLE"

  validation {
    condition     = contains(["MUTABLE", "IMMUTABLE"], var.image_tag_mutability)
    error_message = "image_tag_mutability는 MUTABLE 또는 IMMUTABLE이어야 합니다."
  }
}

variable "scan_on_push" {
  description = "이미지 푸시 시 자동 스캔 활성화 여부"
  type        = bool
  default     = true
}

variable "encryption_type" {
  description = "ECR 암호화 타입"
  type        = string
  default     = "AES256"

  validation {
    condition     = contains(["AES256", "KMS"], var.encryption_type)
    error_message = "encryption_type은 AES256 또는 KMS여야 합니다."
  }
}

variable "kms_key_id" {
  description = "KMS 키 ID (encryption_type이 KMS인 경우)"
  type        = string
  default     = null
}

variable "force_delete" {
  description = "리포지토리 강제 삭제 허용 여부"
  type        = bool
  default     = false
}

variable "max_image_count" {
  description = "유지할 최대 이미지 수"
  type        = number
  default     = 10

  validation {
    condition     = var.max_image_count > 0 && var.max_image_count <= 1000
    error_message = "max_image_count는 1과 1000 사이의 값이어야 합니다."
  }
}

variable "untagged_image_days" {
  description = "태그되지 않은 이미지 보관 일수"
  type        = number
  default     = 7

  validation {
    condition     = var.untagged_image_days > 0 && var.untagged_image_days <= 365
    error_message = "untagged_image_days는 1과 365 사이의 값이어야 합니다."
  }
}

variable "repository_policy" {
  description = "ECR 리포지토리 정책 JSON"
  type        = string
  default     = null
}

variable "create_otel_repository" {
  description = "OpenTelemetry Collector용 별도 리포지토리 생성 여부"
  type        = bool
  default     = true
}

variable "environment" {
  description = "배포 환경 (dev, staging, prod)"
  type        = string

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "environment는 dev, staging, prod 중 하나여야 합니다."
  }
}

variable "common_tags" {
  description = "모든 리소스에 적용할 공통 태그"
  type        = map(string)
  default = {
    Project   = "GamePulse"
    ManagedBy = "Terraform"
    Owner     = "DevOps Team"
  }
}