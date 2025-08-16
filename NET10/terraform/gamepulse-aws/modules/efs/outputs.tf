output "efs_id" {
  description = "EFS 파일 시스템 ID"
  value       = aws_efs_file_system.main.id
}

output "efs_arn" {
  description = "EFS 파일 시스템 ARN"
  value       = aws_efs_file_system.main.arn
}

output "efs_dns_name" {
  description = "EFS 파일 시스템 DNS 이름"
  value       = aws_efs_file_system.main.dns_name
}

output "efs_security_group_id" {
  description = "EFS 보안 그룹 ID"
  value       = aws_security_group.efs.id
}

output "prometheus_access_point_id" {
  description = "Prometheus EFS 액세스 포인트 ID"
  value       = aws_efs_access_point.prometheus.id
}

output "prometheus_access_point_arn" {
  description = "Prometheus EFS 액세스 포인트 ARN"
  value       = aws_efs_access_point.prometheus.arn
}

output "jaeger_access_point_id" {
  description = "Jaeger EFS 액세스 포인트 ID"
  value       = aws_efs_access_point.jaeger.id
}

output "jaeger_access_point_arn" {
  description = "Jaeger EFS 액세스 포인트 ARN"
  value       = aws_efs_access_point.jaeger.arn
}

output "grafana_access_point_id" {
  description = "Grafana EFS 액세스 포인트 ID"
  value       = aws_efs_access_point.grafana.id
}

output "grafana_access_point_arn" {
  description = "Grafana EFS 액세스 포인트 ARN"
  value       = aws_efs_access_point.grafana.arn
}

output "mount_targets" {
  description = "EFS 마운트 타겟 정보"
  value = {
    for idx, mt in aws_efs_mount_target.main : idx => {
      id               = mt.id
      dns_name         = mt.dns_name
      ip_address       = mt.ip_address
      subnet_id        = mt.subnet_id
      availability_zone_name = mt.availability_zone_name
    }
  }
}