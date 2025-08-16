# IAM 모듈 출력값

output "ecs_task_execution_role_arn" {
  description = "ECS 태스크 실행 역할 ARN"
  value       = aws_iam_role.ecs_task_execution_role.arn
}

output "ecs_task_role_arn" {
  description = "ECS 태스크 역할 ARN"
  value       = aws_iam_role.ecs_task_role.arn
}

output "ecs_service_role_arn" {
  description = "ECS 서비스 역할 ARN"
  value       = aws_iam_role.ecs_service_role.arn
}

output "ecs_autoscaling_role_arn" {
  description = "ECS Auto Scaling 역할 ARN"
  value       = aws_iam_role.ecs_autoscaling_role.arn
}

output "codebuild_role_arn" {
  description = "CodeBuild 역할 ARN"
  value       = aws_iam_role.codebuild_role.arn
}

output "monitoring_task_role_arn" {
  description = "모니터링 태스크 역할 ARN"
  value       = aws_iam_role.monitoring_task_role.arn
}

# 역할 이름들도 출력 (다른 리소스에서 참조용)
output "ecs_task_execution_role_name" {
  description = "ECS 태스크 실행 역할 이름"
  value       = aws_iam_role.ecs_task_execution_role.name
}

output "ecs_task_role_name" {
  description = "ECS 태스크 역할 이름"
  value       = aws_iam_role.ecs_task_role.name
}

output "ecs_service_role_name" {
  description = "ECS 서비스 역할 이름"
  value       = aws_iam_role.ecs_service_role.name
}

output "monitoring_task_role_name" {
  description = "모니터링 태스크 역할 이름"
  value       = aws_iam_role.monitoring_task_role.name
}