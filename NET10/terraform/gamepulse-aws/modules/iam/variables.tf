# IAM 모듈 변수 정의

variable "project_name" {
  description = "프로젝트 이름"
  type        = string
}

variable "aws_region" {
  description = "AWS 리전"
  type        = string
}

variable "account_id" {
  description = "AWS 계정 ID"
  type        = string
}