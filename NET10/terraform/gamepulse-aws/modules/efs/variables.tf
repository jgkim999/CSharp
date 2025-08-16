variable "project_name" {
  description = "프로젝트 이름"
  type        = string
}

variable "environment" {
  description = "환경 (dev, staging, prod)"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "private_subnet_ids" {
  description = "프라이빗 서브넷 ID 목록"
  type        = list(string)
}

variable "allowed_security_groups" {
  description = "EFS 접근을 허용할 보안 그룹 ID 목록"
  type        = list(string)
}

variable "performance_mode" {
  description = "EFS 성능 모드 (generalPurpose 또는 maxIO)"
  type        = string
  default     = "generalPurpose"
}

variable "throughput_mode" {
  description = "EFS 처리량 모드 (bursting 또는 provisioned)"
  type        = string
  default     = "bursting"
}

variable "kms_key_id" {
  description = "EFS 암호화에 사용할 KMS 키 ID (선택사항)"
  type        = string
  default     = null
}

variable "transition_to_ia" {
  description = "IA 스토리지 클래스로 전환할 기간"
  type        = string
  default     = "AFTER_30_DAYS"
}

variable "transition_to_primary_storage_class" {
  description = "기본 스토리지 클래스로 다시 전환할 조건"
  type        = string
  default     = "AFTER_1_ACCESS"
}

variable "tags" {
  description = "리소스에 적용할 태그"
  type        = map(string)
  default     = {}
}