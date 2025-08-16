# ECS 클러스터 모듈 변수 정의

variable "cluster_name" {
  description = "ECS 클러스터 이름"
  type        = string
  validation {
    condition     = length(var.cluster_name) > 0 && length(var.cluster_name) <= 255
    error_message = "클러스터 이름은 1-255자 사이여야 합니다."
  }
}

variable "log_retention_days" {
  description = "CloudWatch 로그 보존 기간 (일)"
  type        = number
  default     = 30
  validation {
    condition = contains([
      1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365, 400, 545, 731, 1827, 3653
    ], var.log_retention_days)
    error_message = "로그 보존 기간은 유효한 CloudWatch 로그 보존 기간이어야 합니다."
  }
}

variable "tags" {
  description = "리소스에 적용할 태그"
  type        = map(string)
  default     = {}
}

variable "enable_container_insights" {
  description = "Container Insights 활성화 여부"
  type        = bool
  default     = true
}

variable "enable_execute_command" {
  description = "ECS Exec 명령 실행 활성화 여부"
  type        = bool
  default     = true
}