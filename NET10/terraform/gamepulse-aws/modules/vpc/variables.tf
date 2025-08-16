# VPC 모듈 변수 정의

variable "project_name" {
  description = "프로젝트 이름"
  type        = string
}

variable "vpc_cidr" {
  description = "VPC CIDR 블록"
  type        = string
}

variable "availability_zones" {
  description = "사용할 가용 영역 수"
  type        = number
}

variable "aws_region" {
  description = "AWS 리전"
  type        = string
}

# 데이터 소스: 가용 영역 정보
data "aws_availability_zones" "available" {
  state = "available"
}