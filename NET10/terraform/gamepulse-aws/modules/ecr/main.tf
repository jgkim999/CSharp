# ECR 리포지토리 모듈
# GamePulse 애플리케이션을 위한 ECR 리포지토리 생성 및 구성

terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# GamePulse 애플리케이션용 ECR 리포지토리
resource "aws_ecr_repository" "gamepulse" {
  name                 = var.repository_name
  image_tag_mutability = var.image_tag_mutability
  force_delete         = var.force_delete

  # 이미지 스캔 설정 (요구사항 1.4)
  image_scanning_configuration {
    scan_on_push = var.scan_on_push
  }

  # 암호화 설정
  encryption_configuration {
    encryption_type = var.encryption_type
    kms_key         = var.kms_key_id
  }

  tags = merge(var.common_tags, {
    Name        = var.repository_name
    Component   = "Container Registry"
    Environment = var.environment
  })
}

# ECR 리포지토리 정책 (필요시 외부 접근 제어)
resource "aws_ecr_repository_policy" "gamepulse_policy" {
  count      = var.repository_policy != null ? 1 : 0
  repository = aws_ecr_repository.gamepulse.name
  policy     = var.repository_policy
}

# 라이프사이클 정책 (요구사항 1.4)
resource "aws_ecr_lifecycle_policy" "gamepulse_lifecycle" {
  repository = aws_ecr_repository.gamepulse.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Keep last ${var.max_image_count} images"
        selection = {
          tagStatus     = "tagged"
          tagPrefixList = ["v"]
          countType     = "imageCountMoreThan"
          countNumber   = var.max_image_count
        }
        action = {
          type = "expire"
        }
      },
      {
        rulePriority = 2
        description  = "Delete untagged images older than ${var.untagged_image_days} days"
        selection = {
          tagStatus   = "untagged"
          countType   = "sinceImagePushed"
          countUnit   = "days"
          countNumber = var.untagged_image_days
        }
        action = {
          type = "expire"
        }
      },
      {
        rulePriority = 3
        description  = "Keep only latest image for each tag prefix"
        selection = {
          tagStatus     = "tagged"
          tagPrefixList = ["latest", "main", "develop"]
          countType     = "imageCountMoreThan"
          countNumber   = 1
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}

# OpenTelemetry Collector용 ECR 리포지토리 (사이드카용)
resource "aws_ecr_repository" "otel_collector" {
  count                = var.create_otel_repository ? 1 : 0
  name                 = "${var.repository_name}-otel-collector"
  image_tag_mutability = var.image_tag_mutability
  force_delete         = var.force_delete

  # 이미지 스캔 설정
  image_scanning_configuration {
    scan_on_push = var.scan_on_push
  }

  # 암호화 설정
  encryption_configuration {
    encryption_type = var.encryption_type
    kms_key         = var.kms_key_id
  }

  tags = merge(var.common_tags, {
    Name        = "${var.repository_name}-otel-collector"
    Component   = "Observability"
    Environment = var.environment
  })
}

# OpenTelemetry Collector 라이프사이클 정책
resource "aws_ecr_lifecycle_policy" "otel_collector_lifecycle" {
  count      = var.create_otel_repository ? 1 : 0
  repository = aws_ecr_repository.otel_collector[0].name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Keep last 5 images for otel collector"
        selection = {
          tagStatus   = "any"
          countType   = "imageCountMoreThan"
          countNumber = 5
        }
        action = {
          type = "expire"
        }
      },
      {
        rulePriority = 2
        description  = "Delete untagged images older than 1 day"
        selection = {
          tagStatus   = "untagged"
          countType   = "sinceImagePushed"
          countUnit   = "days"
          countNumber = 1
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}