# ALB 모듈 변수 정의

variable "project_name" {
  description = "프로젝트 이름 (리소스 명명에 사용)"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "public_subnet_ids" {
  description = "ALB가 배치될 퍼블릭 서브넷 ID 목록"
  type        = list(string)
}

variable "ecs_security_group_id" {
  description = "ECS 보안 그룹 ID (ALB에서 ECS로의 트래픽 허용용)"
  type        = string
}

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

variable "health_check_path" {
  description = "헬스 체크 경로"
  type        = string
  default     = "/health"
}

variable "ssl_policy" {
  description = "SSL 보안 정책"
  type        = string
  default     = "ELBSecurityPolicy-TLS-1-2-2017-01"
}

variable "enable_deletion_protection" {
  description = "ALB 삭제 보호 활성화 여부"
  type        = bool
  default     = false
}

variable "common_tags" {
  description = "모든 리소스에 적용할 공통 태그"
  type        = map(string)
  default     = {}
}

# 타겟 그룹 설정
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
}

# 리스너 규칙 설정
variable "listener_rules" {
  description = "추가 리스너 규칙 설정"
  type = list(object({
    priority = number
    conditions = list(object({
      field  = string
      values = list(string)
    }))
    actions = list(object({
      type             = string
      target_group_arn = string
      redirect_config = optional(object({
        host        = string
        path        = string
        port        = string
        protocol    = string
        query       = string
        status_code = string
      }))
    }))
  }))
  default = []
}

# 모니터링 설정
variable "enable_access_logs" {
  description = "ALB 액세스 로그 활성화 여부"
  type        = bool
  default     = false
}

variable "access_logs_bucket" {
  description = "ALB 액세스 로그를 저장할 S3 버킷 이름"
  type        = string
  default     = ""
}

variable "access_logs_prefix" {
  description = "ALB 액세스 로그 S3 접두사"
  type        = string
  default     = "alb-logs"
}

# 추가 기능 변수들
variable "create_monitoring_target_groups" {
  description = "모니터링 서비스용 타겟 그룹 생성 여부"
  type        = bool
  default     = false
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
}

# WAF 설정
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

# 고급 보안 설정
variable "drop_invalid_header_fields" {
  description = "잘못된 헤더 필드 삭제 여부"
  type        = bool
  default     = true
}

variable "enable_http2" {
  description = "HTTP/2 프로토콜 활성화 여부"
  type        = bool
  default     = true
}

variable "idle_timeout" {
  description = "ALB 유휴 타임아웃 (초)"
  type        = number
  default     = 60
}

# 크로스 존 로드 밸런싱
variable "enable_cross_zone_load_balancing" {
  description = "크로스 존 로드 밸런싱 활성화 여부"
  type        = bool
  default     = true
}

# IP 주소 타입
variable "ip_address_type" {
  description = "ALB IP 주소 타입 (ipv4 또는 dualstack)"
  type        = string
  default     = "ipv4"

  validation {
    condition     = contains(["ipv4", "dualstack"], var.ip_address_type)
    error_message = "IP 주소 타입은 'ipv4' 또는 'dualstack'이어야 합니다."
  }
}

# 추가 SSL 정책 옵션
variable "ssl_policy_options" {
  description = "사용 가능한 SSL 정책 목록"
  type        = list(string)
  default = [
    "ELBSecurityPolicy-TLS-1-2-2017-01",
    "ELBSecurityPolicy-TLS-1-2-Ext-2018-06",
    "ELBSecurityPolicy-FS-1-2-Res-2020-10",
    "ELBSecurityPolicy-TLS-1-1-2017-01"
  ]
}