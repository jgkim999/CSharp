# 프로덕션 ALB 구성 예제
# HTTPS, 액세스 로그, WAF, 모니터링 포함

# S3 버킷 (액세스 로그용)
resource "aws_s3_bucket" "alb_logs" {
  bucket = "gamepulse-prod-alb-logs-${random_id.bucket_suffix.hex}"
}

resource "aws_s3_bucket_policy" "alb_logs" {
  bucket = aws_s3_bucket.alb_logs.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          AWS = "arn:aws:iam::600734575887:root" # ELB 서비스 계정 (ap-northeast-2)
        }
        Action   = "s3:PutObject"
        Resource = "${aws_s3_bucket.alb_logs.arn}/alb-logs/AWSLogs/*"
      },
      {
        Effect = "Allow"
        Principal = {
          Service = "delivery.logs.amazonaws.com"
        }
        Action   = "s3:PutObject"
        Resource = "${aws_s3_bucket.alb_logs.arn}/alb-logs/AWSLogs/*"
        Condition = {
          StringEquals = {
            "s3:x-amz-acl" = "bucket-owner-full-control"
          }
        }
      }
    ]
  })
}

resource "random_id" "bucket_suffix" {
  byte_length = 4
}

# WAF Web ACL
resource "aws_wafv2_web_acl" "main" {
  name  = "gamepulse-prod-waf"
  scope = "REGIONAL"

  default_action {
    allow {}
  }

  rule {
    name     = "RateLimitRule"
    priority = 1

    override_action {
      none {}
    }

    statement {
      rate_based_statement {
        limit              = 2000
        aggregate_key_type = "IP"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "RateLimitRule"
      sampled_requests_enabled   = true
    }

    action {
      block {}
    }
  }

  visibility_config {
    cloudwatch_metrics_enabled = true
    metric_name                = "gamepulse-prod-waf"
    sampled_requests_enabled   = true
  }

  tags = {
    Environment = "production"
    Project     = "gamepulse"
  }
}

# 프로덕션 ALB 구성
module "alb_production" {
  source = "../"

  project_name          = "gamepulse-prod"
  vpc_id                = var.vpc_id
  public_subnet_ids     = var.public_subnet_ids
  ecs_security_group_id = var.ecs_security_group_id

  # SSL 설정
  domain_name = "api.gamepulse.com"
  subject_alternative_names = [
    "www.api.gamepulse.com",
    "*.api.gamepulse.com"
  ]
  ssl_policy = "ELBSecurityPolicy-TLS-1-2-2017-01"

  # 액세스 로그 설정
  enable_access_logs = true
  access_logs_bucket = aws_s3_bucket.alb_logs.bucket
  access_logs_prefix = "alb-logs"

  # WAF 설정
  enable_waf  = true
  waf_acl_arn = aws_wafv2_web_acl.main.arn

  # 고급 설정
  enable_deletion_protection       = true
  enable_http2                     = true
  enable_cross_zone_load_balancing = true
  idle_timeout                     = 60
  drop_invalid_header_fields       = true

  # 헬스 체크 설정
  target_group_health_check = {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200"
    path                = "/health"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 3
  }

  # 모니터링 타겟 그룹 생성
  create_monitoring_target_groups = true

  # 리스너 규칙 (API 버전별 라우팅)
  listener_rules = [
    {
      priority = 100
      conditions = [
        {
          field  = "path-pattern"
          values = ["/api/v1/*"]
        }
      ]
      actions = [
        {
          type             = "forward"
          target_group_arn = module.alb_production.target_group_arn
        }
      ]
    },
    {
      priority = 200
      conditions = [
        {
          field  = "host-header"
          values = ["monitoring.gamepulse.com"]
        }
      ]
      actions = [
        {
          type             = "forward"
          target_group_arn = module.alb_production.target_group_arn # 모니터링 타겟 그룹으로 변경 필요
        }
      ]
    }
  ]

  # 프로덕션 태그
  common_tags = {
    Environment = "production"
    Project     = "gamepulse"
    Owner       = "platform-team"
    CostCenter  = "engineering"
    Backup      = "required"
  }
}

# 변수 정의
variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "public_subnet_ids" {
  description = "퍼블릭 서브넷 ID 목록"
  type        = list(string)
}

variable "ecs_security_group_id" {
  description = "ECS 보안 그룹 ID"
  type        = string
}

# 출력값
output "alb_dns_name" {
  description = "ALB DNS 이름"
  value       = module.alb_production.alb_dns_name
}

output "alb_zone_id" {
  description = "ALB Zone ID"
  value       = module.alb_production.alb_zone_id
}

output "ssl_certificate_arn" {
  description = "SSL 인증서 ARN"
  value       = module.alb_production.ssl_certificate_arn
}

output "target_group_arn" {
  description = "타겟 그룹 ARN"
  value       = module.alb_production.target_group_arn
}

output "waf_web_acl_arn" {
  description = "WAF Web ACL ARN"
  value       = aws_wafv2_web_acl.main.arn
}