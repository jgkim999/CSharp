# ECS 클러스터 모듈
# Fargate 기반 ECS 클러스터 생성 및 Container Insights 활성화

# ECS 클러스터 생성
resource "aws_ecs_cluster" "main" {
  name = var.cluster_name

  # Container Insights 활성화
  setting {
    name  = "containerInsights"
    value = "enabled"
  }

  # 클러스터 로깅 구성
  configuration {
    execute_command_configuration {
      logging = "OVERRIDE"

      log_configuration {
        cloud_watch_encryption_enabled = true
        cloud_watch_log_group_name     = aws_cloudwatch_log_group.ecs_cluster.name
      }
    }
  }

  tags = merge(var.tags, {
    Name = var.cluster_name
    Type = "ECS-Cluster"
  })
}

# ECS 클러스터 로깅을 위한 CloudWatch 로그 그룹
resource "aws_cloudwatch_log_group" "ecs_cluster" {
  name              = "/aws/ecs/cluster/${var.cluster_name}"
  retention_in_days = var.log_retention_days

  tags = merge(var.tags, {
    Name = "${var.cluster_name}-cluster-logs"
    Type = "CloudWatch-LogGroup"
  })
}

# ECS 클러스터 용량 공급자 (Fargate)
resource "aws_ecs_cluster_capacity_providers" "main" {
  cluster_name = aws_ecs_cluster.main.name

  capacity_providers = ["FARGATE", "FARGATE_SPOT"]

  default_capacity_provider_strategy {
    base              = 1
    weight            = 100
    capacity_provider = "FARGATE"
  }

  default_capacity_provider_strategy {
    base              = 0
    weight            = 0
    capacity_provider = "FARGATE_SPOT"
  }
}

# ECS 서비스용 CloudWatch 로그 그룹들
resource "aws_cloudwatch_log_group" "gamepulse_app" {
  name              = "/aws/ecs/${var.cluster_name}/gamepulse-app"
  retention_in_days = var.log_retention_days

  tags = merge(var.tags, {
    Name = "${var.cluster_name}-gamepulse-app-logs"
    Type = "CloudWatch-LogGroup"
  })
}

resource "aws_cloudwatch_log_group" "otel_collector" {
  name              = "/aws/ecs/${var.cluster_name}/otel-collector"
  retention_in_days = var.log_retention_days

  tags = merge(var.tags, {
    Name = "${var.cluster_name}-otel-collector-logs"
    Type = "CloudWatch-LogGroup"
  })
}

resource "aws_cloudwatch_log_group" "prometheus" {
  name              = "/aws/ecs/${var.cluster_name}/prometheus"
  retention_in_days = var.log_retention_days

  tags = merge(var.tags, {
    Name = "${var.cluster_name}-prometheus-logs"
    Type = "CloudWatch-LogGroup"
  })
}

resource "aws_cloudwatch_log_group" "loki" {
  name              = "/aws/ecs/${var.cluster_name}/loki"
  retention_in_days = var.log_retention_days

  tags = merge(var.tags, {
    Name = "${var.cluster_name}-loki-logs"
    Type = "CloudWatch-LogGroup"
  })
}

resource "aws_cloudwatch_log_group" "jaeger" {
  name              = "/aws/ecs/${var.cluster_name}/jaeger"
  retention_in_days = var.log_retention_days

  tags = merge(var.tags, {
    Name = "${var.cluster_name}-jaeger-logs"
    Type = "CloudWatch-LogGroup"
  })
}

resource "aws_cloudwatch_log_group" "grafana" {
  name              = "/aws/ecs/${var.cluster_name}/grafana"
  retention_in_days = var.log_retention_days

  tags = merge(var.tags, {
    Name = "${var.cluster_name}-grafana-logs"
    Type = "CloudWatch-LogGroup"
  })
}