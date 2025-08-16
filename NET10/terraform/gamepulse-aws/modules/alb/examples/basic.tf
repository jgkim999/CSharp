# 기본 ALB 구성 예제
# HTTP만 사용하는 간단한 설정

module "alb_basic" {
  source = "../"

  project_name          = "gamepulse-dev"
  vpc_id                = "vpc-12345678"
  public_subnet_ids     = ["subnet-12345678", "subnet-87654321"]
  ecs_security_group_id = "sg-12345678"

  # 기본 헬스 체크 설정
  health_check_path = "/health"

  # 개발 환경용 태그
  common_tags = {
    Environment = "development"
    Project     = "gamepulse"
    Owner       = "dev-team"
  }
}

# 출력값 예제
output "alb_dns_name" {
  description = "ALB DNS 이름"
  value       = module.alb_basic.alb_dns_name
}

output "target_group_arn" {
  description = "타겟 그룹 ARN"
  value       = module.alb_basic.target_group_arn
}