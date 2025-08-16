# 보안 그룹 모듈 출력값

output "alb_security_group_id" {
  description = "ALB 보안 그룹 ID"
  value       = aws_security_group.alb.id
}

output "ecs_security_group_id" {
  description = "ECS 보안 그룹 ID"
  value       = aws_security_group.ecs.id
}

output "monitoring_security_group_id" {
  description = "모니터링 보안 그룹 ID"
  value       = aws_security_group.monitoring.id
}

output "rds_security_group_id" {
  description = "RDS 보안 그룹 ID"
  value       = aws_security_group.rds.id
}

output "elasticache_security_group_id" {
  description = "ElastiCache 보안 그룹 ID"
  value       = aws_security_group.elasticache.id
}

output "efs_security_group_id" {
  description = "EFS 보안 그룹 ID"
  value       = aws_security_group.efs.id
}