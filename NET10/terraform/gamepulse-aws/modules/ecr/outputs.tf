# ECR 모듈 출력값 정의

output "repository_url" {
  description = "GamePulse ECR 리포지토리 URL"
  value       = aws_ecr_repository.gamepulse.repository_url
}

output "repository_arn" {
  description = "GamePulse ECR 리포지토리 ARN"
  value       = aws_ecr_repository.gamepulse.arn
}

output "repository_name" {
  description = "GamePulse ECR 리포지토리 이름"
  value       = aws_ecr_repository.gamepulse.name
}

output "registry_id" {
  description = "ECR 레지스트리 ID"
  value       = aws_ecr_repository.gamepulse.registry_id
}

output "otel_collector_repository_url" {
  description = "OpenTelemetry Collector ECR 리포지토리 URL"
  value       = var.create_otel_repository ? aws_ecr_repository.otel_collector[0].repository_url : null
}

output "otel_collector_repository_arn" {
  description = "OpenTelemetry Collector ECR 리포지토리 ARN"
  value       = var.create_otel_repository ? aws_ecr_repository.otel_collector[0].arn : null
}

output "otel_collector_repository_name" {
  description = "OpenTelemetry Collector ECR 리포지토리 이름"
  value       = var.create_otel_repository ? aws_ecr_repository.otel_collector[0].name : null
}

# Docker 로그인 명령어 (참고용)
output "docker_login_command" {
  description = "ECR에 Docker 로그인하기 위한 AWS CLI 명령어"
  value       = "aws ecr get-login-password --region ${data.aws_region.current.name} | docker login --username AWS --password-stdin ${aws_ecr_repository.gamepulse.repository_url}"
}

# 이미지 빌드 및 푸시 예제 명령어 (참고용)
output "docker_build_and_push_commands" {
  description = "Docker 이미지 빌드 및 푸시 명령어 예제"
  value = {
    build = "docker build -t ${aws_ecr_repository.gamepulse.name} ."
    tag   = "docker tag ${aws_ecr_repository.gamepulse.name}:latest ${aws_ecr_repository.gamepulse.repository_url}:latest"
    push  = "docker push ${aws_ecr_repository.gamepulse.repository_url}:latest"
  }
}

# 현재 AWS 리전 정보
data "aws_region" "current" {}

# 현재 AWS 계정 정보
data "aws_caller_identity" "current" {}

output "aws_account_id" {
  description = "현재 AWS 계정 ID"
  value       = data.aws_caller_identity.current.account_id
}

output "aws_region" {
  description = "현재 AWS 리전"
  value       = data.aws_region.current.name
}