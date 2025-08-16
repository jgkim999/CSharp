# ECS 클러스터 모듈 출력값

output "cluster_id" {
  description = "ECS 클러스터 ID"
  value       = aws_ecs_cluster.main.id
}

output "cluster_arn" {
  description = "ECS 클러스터 ARN"
  value       = aws_ecs_cluster.main.arn
}

output "cluster_name" {
  description = "ECS 클러스터 이름"
  value       = aws_ecs_cluster.main.name
}

output "cluster_endpoint" {
  description = "ECS 클러스터 엔드포인트"
  value       = aws_ecs_cluster.main.arn
}

# CloudWatch 로그 그룹 출력값
output "log_groups" {
  description = "생성된 CloudWatch 로그 그룹들"
  value = {
    cluster        = aws_cloudwatch_log_group.ecs_cluster.name
    gamepulse_app  = aws_cloudwatch_log_group.gamepulse_app.name
    otel_collector = aws_cloudwatch_log_group.otel_collector.name
    prometheus     = aws_cloudwatch_log_group.prometheus.name
    loki           = aws_cloudwatch_log_group.loki.name
    jaeger         = aws_cloudwatch_log_group.jaeger.name
    grafana        = aws_cloudwatch_log_group.grafana.name
  }
}

output "log_group_arns" {
  description = "CloudWatch 로그 그룹 ARN들"
  value = {
    cluster        = aws_cloudwatch_log_group.ecs_cluster.arn
    gamepulse_app  = aws_cloudwatch_log_group.gamepulse_app.arn
    otel_collector = aws_cloudwatch_log_group.otel_collector.arn
    prometheus     = aws_cloudwatch_log_group.prometheus.arn
    loki           = aws_cloudwatch_log_group.loki.arn
    jaeger         = aws_cloudwatch_log_group.jaeger.arn
    grafana        = aws_cloudwatch_log_group.grafana.arn
  }
}

output "capacity_providers" {
  description = "ECS 클러스터 용량 공급자"
  value       = aws_ecs_cluster_capacity_providers.main.capacity_providers
}