# GamePulse AWS 배포를 위한 메인 Terraform 구성
# 요구사항: 8.1, 8.3

terraform {
  required_version = ">= 1.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # S3 백엔드 상태 관리 구성 (실제 배포 시 활성화)
  # backend "s3" {
  #   bucket         = "gamepulse-terraform-state"
  #   key            = "gamepulse/terraform.tfstate"
  #   region         = "ap-northeast-2"
  #   encrypt        = true
  #   dynamodb_table = "gamepulse-terraform-locks"
  # }
}

# AWS 프로바이더 설정
provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "GamePulse"
      Environment = var.environment
      ManagedBy   = "Terraform"
    }
  }
}

# 데이터 소스: 현재 AWS 계정 정보
data "aws_caller_identity" "current" {}

# 데이터 소스: 가용 영역 정보
data "aws_availability_zones" "available" {
  state = "available"
}

# VPC 모듈
module "vpc" {
  source = "./modules/vpc"

  project_name       = var.project_name
  vpc_cidr           = var.vpc_cidr
  availability_zones = var.availability_zones
  aws_region         = var.aws_region
}

# 보안 그룹 모듈
module "security_groups" {
  source = "./modules/security_groups"

  project_name = var.project_name
  vpc_id       = module.vpc.vpc_id
  vpc_cidr     = var.vpc_cidr

  depends_on = [module.vpc]
}

# IAM 모듈
module "iam" {
  source = "./modules/iam"

  project_name = var.project_name
  aws_region   = var.aws_region
  account_id   = data.aws_caller_identity.current.account_id
}

# ECR 모듈
module "ecr" {
  source = "./modules/ecr"

  repository_name        = var.ecr_repository_name
  environment            = var.environment
  image_tag_mutability   = var.ecr_image_tag_mutability
  scan_on_push           = var.ecr_scan_on_push
  encryption_type        = var.ecr_encryption_type
  kms_key_id             = var.ecr_kms_key_id
  max_image_count        = var.ecr_max_image_count
  untagged_image_days    = var.ecr_untagged_image_days
  create_otel_repository = var.ecr_create_otel_repository
  force_delete           = var.ecr_force_delete

  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "Terraform"
    Owner       = "DevOps Team"
  }
}

# ECS 클러스터 모듈
module "ecs" {
  source = "./modules/ecs"

  cluster_name              = "${var.project_name}-${var.environment}"
  log_retention_days        = var.ecs_log_retention_days
  enable_container_insights = var.ecs_enable_container_insights
  enable_execute_command    = var.ecs_enable_execute_command

  tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "Terraform"
    Owner       = "DevOps Team"
  }

  depends_on = [module.vpc, module.iam]
}

# ALB 모듈
module "alb" {
  source = "./modules/alb"

  project_name          = var.project_name
  vpc_id                = module.vpc.vpc_id
  public_subnet_ids     = module.vpc.public_subnets
  ecs_security_group_id = module.security_groups.ecs_security_group_id

  # SSL 설정 (도메인이 제공된 경우)
  domain_name               = var.domain_name
  subject_alternative_names = var.subject_alternative_names
  ssl_policy                = var.ssl_policy

  # 헬스 체크 설정
  health_check_path = var.health_check_path

  # 액세스 로그 설정
  enable_access_logs = var.enable_alb_access_logs
  access_logs_bucket = var.alb_access_logs_bucket
  access_logs_prefix = var.alb_access_logs_prefix

  # WAF 설정
  enable_waf  = var.enable_waf
  waf_acl_arn = var.waf_acl_arn

  # 고급 설정
  enable_deletion_protection       = var.enable_alb_deletion_protection
  enable_http2                     = var.enable_http2
  enable_cross_zone_load_balancing = var.enable_cross_zone_load_balancing
  idle_timeout                     = var.alb_idle_timeout
  drop_invalid_header_fields       = var.drop_invalid_header_fields

  # 세션 스티키니스
  enable_stickiness   = var.enable_stickiness
  stickiness_duration = var.stickiness_duration

  # 모니터링 타겟 그룹
  create_monitoring_target_groups = var.create_monitoring_target_groups

  # 타겟 그룹 헬스 체크 설정
  target_group_health_check = var.target_group_health_check

  # 공통 태그
  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "Terraform"
    Owner       = "DevOps Team"
  }

  depends_on = [module.vpc, module.security_groups]
}

# EFS 모듈
module "efs" {
  source = "./modules/efs"

  project_name           = var.project_name
  environment            = var.environment
  vpc_id                 = module.vpc.vpc_id
  private_subnet_ids     = module.vpc.private_subnets
  allowed_security_groups = [module.security_groups.ecs_security_group_id]

  # EFS 성능 설정
  performance_mode = var.efs_performance_mode
  throughput_mode  = var.efs_throughput_mode

  # 암호화 설정
  kms_key_id = var.efs_kms_key_id

  # 라이프사이클 정책
  transition_to_ia                        = var.efs_transition_to_ia
  transition_to_primary_storage_class     = var.efs_transition_to_primary_storage_class

  tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "Terraform"
    Owner       = "DevOps Team"
  }

  depends_on = [module.vpc, module.security_groups]
}

# S3 모듈
module "s3" {
  source = "./modules/s3"

  project_name      = var.project_name
  environment       = var.environment
  enable_versioning = var.s3_enable_versioning
  kms_key_id        = var.s3_kms_key_id

  # Loki 로그 라이프사이클 설정
  loki_logs_ia_transition_days                    = var.s3_loki_logs_ia_transition_days
  loki_logs_glacier_transition_days               = var.s3_loki_logs_glacier_transition_days
  loki_logs_deep_archive_transition_days          = var.s3_loki_logs_deep_archive_transition_days
  loki_logs_expiration_days                       = var.s3_loki_logs_expiration_days
  loki_logs_noncurrent_version_expiration_days    = var.s3_loki_logs_noncurrent_version_expiration_days

  # 백업 라이프사이클 설정
  backups_ia_transition_days                      = var.s3_backups_ia_transition_days
  backups_glacier_transition_days                 = var.s3_backups_glacier_transition_days
  backups_deep_archive_transition_days            = var.s3_backups_deep_archive_transition_days
  backups_expiration_days                         = var.s3_backups_expiration_days
  backups_noncurrent_version_expiration_days      = var.s3_backups_noncurrent_version_expiration_days



  tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "Terraform"
    Owner       = "DevOps Team"
  }
}