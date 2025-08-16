# 개발 환경 변수 설정

environment  = "dev"
project_name = "gamepulse-dev"

# 개발 환경에서는 최소 리소스 사용
app_cpu           = 512
app_memory        = 1024
app_desired_count = 1

monitoring_cpu    = 256
monitoring_memory = 512

# ECR 설정 (개발)
ecr_repository_name        = "gamepulse-dev"
ecr_image_tag_mutability   = "MUTABLE" # 개발에서는 변경 가능한 태그 허용
ecr_scan_on_push           = false     # 개발에서는 스캔 비활성화로 속도 향상
ecr_encryption_type        = "AES256"
ecr_max_image_count        = 5     # 개발에서는 적은 이미지만 보관
ecr_untagged_image_days    = 1     # 개발에서는 빠른 정리
ecr_create_otel_repository = false # 개발에서는 OpenTelemetry 리포지토리 생략
ecr_force_delete           = true  # 개발에서는 강제 삭제 허용

# 개발 환경에서는 보호 기능 비활성화
enable_deletion_protection = false
backup_retention_days      = 7