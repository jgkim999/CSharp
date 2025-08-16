# 스테이징 환경 변수 설정

environment  = "staging"
project_name = "gamepulse-staging"

# 스테이징 환경에서는 프로덕션과 유사하지만 약간 축소
app_cpu           = 1024
app_memory        = 2048
app_desired_count = 2

monitoring_cpu    = 512
monitoring_memory = 1024

# ECR 설정 (스테이징)
ecr_repository_name        = "gamepulse-staging"
ecr_image_tag_mutability   = "MUTABLE" # 스테이징에서는 변경 가능한 태그 허용
ecr_scan_on_push           = true
ecr_encryption_type        = "AES256"
ecr_max_image_count        = 15
ecr_untagged_image_days    = 5
ecr_create_otel_repository = true
ecr_force_delete           = true # 스테이징에서는 강제 삭제 허용

# ECS 클러스터 설정 (스테이징)
ecs_log_retention_days        = 14
ecs_enable_container_insights = true
ecs_enable_execute_command    = true

# 스테이징 환경에서는 삭제 보호 비활성화
enable_deletion_protection = false
backup_retention_days      = 14

# ALB 설정 (스테이징)
domain_name               = "" # 스테이징 도메인 (예: "staging-api.gamepulse.com")
subject_alternative_names = []
ssl_policy                = "ELBSecurityPolicy-TLS-1-2-2017-01"
health_check_path         = "/health"

# 스테이징에서는 액세스 로그 선택적
enable_alb_access_logs = false
alb_access_logs_bucket = ""
alb_access_logs_prefix = "staging-alb-logs"

# WAF 설정 (스테이징에서는 선택적)
enable_waf  = false
waf_acl_arn = ""

# 스테이징 ALB 설정
enable_alb_deletion_protection   = false
enable_http2                     = true
enable_cross_zone_load_balancing = true
alb_idle_timeout                 = 60
drop_invalid_header_fields       = true

# 세션 스티키니스
enable_stickiness   = false
stickiness_duration = 86400

# 모니터링 타겟 그룹
create_monitoring_target_groups = true

# 타겟 그룹 헬스 체크 (스테이징)
target_group_health_check = {
  enabled             = true
  healthy_threshold   = 2
  interval            = 30
  matcher             = "200"
  path                = "/health"
  port                = "traffic-port"
  protocol            = "HTTP"
  timeout             = 5
  unhealthy_threshold = 2
}

# EFS 설정 (스테이징)
efs_performance_mode                    = "generalPurpose"
efs_throughput_mode                     = "bursting"
efs_kms_key_id                          = null
efs_transition_to_ia                    = "AFTER_30_DAYS"
efs_transition_to_primary_storage_class = "AFTER_1_ACCESS"

# S3 설정 (스테이징)
s3_enable_versioning = true
s3_kms_key_id        = null

# Loki 로그 S3 라이프사이클 (스테이징 - 단기 보관)
s3_loki_logs_ia_transition_days                    = 30  # 30일 후 Standard-IA
s3_loki_logs_glacier_transition_days               = 90  # 90일 후 Glacier
s3_loki_logs_deep_archive_transition_days          = 180 # 6개월 후 Deep Archive
s3_loki_logs_expiration_days                       = 365 # 1년 후 만료 (스테이징은 단기)
s3_loki_logs_noncurrent_version_expiration_days    = 30  # 이전 버전 30일 후 만료

# 백업 S3 라이프사이클 (스테이징 - 단기 보관)
s3_backups_ia_transition_days                      = 30  # 30일 후 Standard-IA
s3_backups_glacier_transition_days                 = 90  # 90일 후 Glacier
s3_backups_deep_archive_transition_days            = 180 # 6개월 후 Deep Archive
s3_backups_expiration_days                         = 730 # 2년 후 만료 (스테이징)
s3_backups_noncurrent_version_expiration_days      = 90  # 이전 버전 90일 후 만료

