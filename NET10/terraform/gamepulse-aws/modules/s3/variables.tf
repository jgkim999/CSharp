variable "project_name" {
  description = "프로젝트 이름"
  type        = string
}

variable "environment" {
  description = "환경 (dev, staging, prod)"
  type        = string
}

variable "enable_versioning" {
  description = "S3 버킷 버전 관리 활성화 여부"
  type        = bool
  default     = true
}

variable "kms_key_id" {
  description = "S3 암호화에 사용할 KMS 키 ID (선택사항)"
  type        = string
  default     = null
}

# Loki 로그 라이프사이클 설정
variable "loki_logs_ia_transition_days" {
  description = "Loki 로그를 Standard-IA로 전환할 일수"
  type        = number
  default     = 30
}

variable "loki_logs_glacier_transition_days" {
  description = "Loki 로그를 Glacier로 전환할 일수"
  type        = number
  default     = 90
}

variable "loki_logs_deep_archive_transition_days" {
  description = "Loki 로그를 Deep Archive로 전환할 일수"
  type        = number
  default     = 365
}

variable "loki_logs_expiration_days" {
  description = "Loki 로그 만료 일수"
  type        = number
  default     = 2555  # 약 7년
}

variable "loki_logs_noncurrent_version_expiration_days" {
  description = "Loki 로그 이전 버전 만료 일수"
  type        = number
  default     = 90
}

# 백업 라이프사이클 설정
variable "backups_ia_transition_days" {
  description = "백업을 Standard-IA로 전환할 일수"
  type        = number
  default     = 30
}

variable "backups_glacier_transition_days" {
  description = "백업을 Glacier로 전환할 일수"
  type        = number
  default     = 90
}

variable "backups_deep_archive_transition_days" {
  description = "백업을 Deep Archive로 전환할 일수"
  type        = number
  default     = 180
}

variable "backups_expiration_days" {
  description = "백업 만료 일수"
  type        = number
  default     = 3650  # 10년
}

variable "backups_noncurrent_version_expiration_days" {
  description = "백업 이전 버전 만료 일수"
  type        = number
  default     = 365
}



variable "tags" {
  description = "리소스에 적용할 태그"
  type        = map(string)
  default     = {}
}