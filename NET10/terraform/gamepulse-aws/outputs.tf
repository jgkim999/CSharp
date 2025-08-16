# GamePulse AWS 배포 출력값 정의
# 요구사항: 8.5

# 계정 정보
output "account_id" {
  description = "AWS 계정 ID"
  value       = data.aws_caller_identity.current.account_id
}

output "region" {
  description = "배포된 AWS 리전"
  value       = var.aws_region
}

# VPC 정보
output "vpc_id" {
  description = "VPC ID"
  value       = module.vpc.vpc_id
}

output "vpc_cidr_block" {
  description = "VPC CIDR 블록"
  value       = module.vpc.vpc_cidr_block
}

output "public_subnet_ids" {
  description = "퍼블릭 서브넷 ID 목록"
  value       = module.vpc.public_subnets
}

output "private_subnet_ids" {
  description = "프라이빗 서브넷 ID 목록"
  value       = module.vpc.private_subnets
}

# ECS 정보
output "ecs_cluster_id" {
  description = "ECS 클러스터 ID"
  value       = module.ecs.cluster_id
}

output "ecs_cluster_arn" {
  description = "ECS 클러스터 ARN"
  value       = module.ecs.cluster_arn
}

output "ecs_cluster_name" {
  description = "ECS 클러스터 이름"
  value       = module.ecs.cluster_name
}

output "ecs_log_groups" {
  description = "ECS CloudWatch 로그 그룹들"
  value       = module.ecs.log_groups
}

output "ecs_capacity_providers" {
  description = "ECS 클러스터 용량 공급자"
  value       = module.ecs.capacity_providers
}

# ALB 정보
output "alb_dns_name" {
  description = "Application Load Balancer DNS 이름"
  value       = module.alb.alb_dns_name
}

output "alb_zone_id" {
  description = "Application Load Balancer Zone ID"
  value       = module.alb.alb_zone_id
}

output "alb_arn" {
  description = "Application Load Balancer ARN"
  value       = module.alb.alb_arn
}

output "alb_security_group_id_from_module" {
  description = "ALB 모듈에서 생성된 보안 그룹 ID"
  value       = module.alb.alb_security_group_id
}

output "target_group_arn" {
  description = "애플리케이션 타겟 그룹 ARN"
  value       = module.alb.target_group_arn
}

output "target_group_name" {
  description = "애플리케이션 타겟 그룹 이름"
  value       = module.alb.target_group_name
}

output "ssl_certificate_arn" {
  description = "SSL 인증서 ARN (도메인이 설정된 경우)"
  value       = module.alb.ssl_certificate_arn
}

output "ssl_certificate_domain_validation_options" {
  description = "SSL 인증서 도메인 검증 옵션"
  value       = module.alb.ssl_certificate_domain_validation_options
  sensitive   = true
}

output "http_listener_arn" {
  description = "HTTP 리스너 ARN"
  value       = module.alb.http_listener_arn
}

output "https_listener_arn" {
  description = "HTTPS 리스너 ARN"
  value       = module.alb.https_listener_arn
}

output "alb_endpoint_url" {
  description = "ALB 엔드포인트 URL"
  value       = var.domain_name != "" ? "https://${var.domain_name}" : "http://${module.alb.alb_dns_name}"
}

# ECR 정보
output "ecr_repository_url" {
  description = "GamePulse ECR 리포지토리 URL"
  value       = module.ecr.repository_url
}

output "ecr_repository_arn" {
  description = "GamePulse ECR 리포지토리 ARN"
  value       = module.ecr.repository_arn
}

output "ecr_repository_name" {
  description = "GamePulse ECR 리포지토리 이름"
  value       = module.ecr.repository_name
}

output "ecr_registry_id" {
  description = "ECR 레지스트리 ID"
  value       = module.ecr.registry_id
}

output "ecr_otel_collector_repository_url" {
  description = "OpenTelemetry Collector ECR 리포지토리 URL"
  value       = module.ecr.otel_collector_repository_url
}

output "ecr_docker_login_command" {
  description = "ECR Docker 로그인 명령어"
  value       = module.ecr.docker_login_command
  sensitive   = true
}

output "ecr_docker_build_and_push_commands" {
  description = "Docker 빌드 및 푸시 명령어 예제"
  value       = module.ecr.docker_build_and_push_commands
}

# IAM 역할 정보
output "ecs_task_execution_role_arn" {
  description = "ECS 태스크 실행 역할 ARN"
  value       = module.iam.ecs_task_execution_role_arn
}

output "ecs_task_role_arn" {
  description = "ECS 태스크 역할 ARN"
  value       = module.iam.ecs_task_role_arn
}

output "ecs_service_role_arn" {
  description = "ECS 서비스 역할 ARN"
  value       = module.iam.ecs_service_role_arn
}

output "ecs_autoscaling_role_arn" {
  description = "ECS Auto Scaling 역할 ARN"
  value       = module.iam.ecs_autoscaling_role_arn
}

output "codebuild_role_arn" {
  description = "CodeBuild 역할 ARN"
  value       = module.iam.codebuild_role_arn
}

output "monitoring_task_role_arn" {
  description = "모니터링 태스크 역할 ARN"
  value       = module.iam.monitoring_task_role_arn
}

# 보안 그룹 정보
output "alb_security_group_id" {
  description = "ALB 보안 그룹 ID"
  value       = module.security_groups.alb_security_group_id
}

output "ecs_security_group_id" {
  description = "ECS 보안 그룹 ID"
  value       = module.security_groups.ecs_security_group_id
}

output "monitoring_security_group_id" {
  description = "모니터링 보안 그룹 ID"
  value       = module.security_groups.monitoring_security_group_id
}

# EFS 정보
output "efs_id" {
  description = "EFS 파일 시스템 ID"
  value       = module.efs.efs_id
}

output "efs_arn" {
  description = "EFS 파일 시스템 ARN"
  value       = module.efs.efs_arn
}

output "efs_dns_name" {
  description = "EFS 파일 시스템 DNS 이름"
  value       = module.efs.efs_dns_name
}

output "efs_security_group_id" {
  description = "EFS 보안 그룹 ID"
  value       = module.efs.efs_security_group_id
}

output "prometheus_access_point_id" {
  description = "Prometheus EFS 액세스 포인트 ID"
  value       = module.efs.prometheus_access_point_id
}

output "jaeger_access_point_id" {
  description = "Jaeger EFS 액세스 포인트 ID"
  value       = module.efs.jaeger_access_point_id
}

output "grafana_access_point_id" {
  description = "Grafana EFS 액세스 포인트 ID"
  value       = module.efs.grafana_access_point_id
}

output "efs_mount_targets" {
  description = "EFS 마운트 타겟 정보"
  value       = module.efs.mount_targets
}

# S3 정보
output "loki_logs_bucket_id" {
  description = "Loki 로그 S3 버킷 ID"
  value       = module.s3.loki_logs_bucket_id
}

output "loki_logs_bucket_arn" {
  description = "Loki 로그 S3 버킷 ARN"
  value       = module.s3.loki_logs_bucket_arn
}

output "backups_bucket_id" {
  description = "백업 S3 버킷 ID"
  value       = module.s3.backups_bucket_id
}

output "backups_bucket_arn" {
  description = "백업 S3 버킷 ARN"
  value       = module.s3.backups_bucket_arn
}

output "s3_buckets" {
  description = "모든 S3 버킷 정보"
  value       = module.s3.buckets
}

# 스토리지 통합 정보
output "storage_configuration" {
  description = "EFS 및 S3 스토리지 구성 정보"
  value = {
    efs = {
      id                        = module.efs.efs_id
      dns_name                  = module.efs.efs_dns_name
      prometheus_access_point   = module.efs.prometheus_access_point_id
      jaeger_access_point       = module.efs.jaeger_access_point_id
      grafana_access_point      = module.efs.grafana_access_point_id
    }
    s3 = {
      loki_logs_bucket = module.s3.loki_logs_bucket_id
      backups_bucket   = module.s3.backups_bucket_id
    }
  }
}