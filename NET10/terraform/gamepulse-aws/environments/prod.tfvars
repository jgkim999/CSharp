# 프로덕션 환경 변수 설정

environment  = "prod"
project_name = "gamepulse"

# 프로덕션 환경에서는 최대 성능과 가용성
app_cpu           = 2048
app_memory        = 4096
app_desired_count = 2

monitoring_cpu    = 1024
monitoring_memory = 2048

# ECR 설정 (프로덕션)
ecr_repository_name        = "gamepulse"
ecr_image_tag_mutability   = "IMMUTABLE" # 프로덕션에서는 불변 태그 사용
ecr_scan_on_push           = true
ecr_encryption_type        = "AES256"
ecr_max_image_count        = 20 # 프로덕션에서는 더 많은 이미지 보관
ecr_untagged_image_days    = 3  # 프로덕션에서는 빠른 정리
ecr_create_otel_repository = true
ecr_force_delete           = false # 프로덕션에서는 강제 삭제 비활성화

# ECS 클러스터 설정 (프로덕션)
ecs_log_retention_days        = 90
ecs_enable_container_insights = true
ecs_enable_execute_command    = false # 프로덕션에서는 보안상 비활성화

# 프로덕션 환경에서는 모든 보호 기능 활성화
enable_deletion_protection = false
backup_retention_days      = 30

# ALB 설정 (프로덕션)
domain_name               = "" # 실제 도메인으로 변경 필요 (예: "api.gamepulse.com")
subject_alternative_names = [] # 추가 도메인이 있는 경우 설정
ssl_policy                = "ELBSecurityPolicy-TLS-1-2-2017-01"
health_check_path         = "/health"

# 프로덕션에서는 액세스 로그 활성화 권장
enable_alb_access_logs = false # S3 버킷 생성 후 true로 변경
alb_access_logs_bucket = ""    # 실제 S3 버킷 이름으로 변경
alb_access_logs_prefix = "alb-logs"

# WAF 설정 (프로덕션에서 권장)
enable_waf  = false # WAF ACL 생성 후 true로 변경
waf_acl_arn = ""    # 실제 WAF ACL ARN으로 변경

# 프로덕션 ALB 고급 설정
enable_alb_deletion_protection   = false # 실제 프로덕션에서는 true 권장
enable_http2                     = true
enable_cross_zone_load_balancing = true
alb_idle_timeout                 = 60
drop_invalid_header_fields       = true

# 세션 스티키니스 (필요한 경우)
enable_stickiness   = false
stickiness_duration = 86400

# 모니터링 타겟 그룹
create_monitoring_target_groups = true

# 타겟 그룹 헬스 체크 (프로덕션 최적화)
target_group_health_check = {
  enabled             = true
  healthy_threshold   = 2
  interval            = 30
  matcher             = "200"
  path                = "/health"
  port                = "traffic-port"
  protocol            = "HTTP"
  timeout             = 5
  unhealthy_threshold = 3 # 프로덕션에서는 더 엄격하게
}

# EFS 설정 (프로덕션)
efs_performance_mode                    = "generalPurpose" # 프로덕션에서는 일반 목적 모드
efs_throughput_mode                     = "bursting"       # 비용 효율적인 버스팅 모드
efs_kms_key_id                          = null             # 필요시 KMS 키 ID 설정
efs_transition_to_ia                    = "AFTER_30_DAYS"  # 30일 후 IA로 전환
efs_transition_to_primary_storage_class = "AFTER_1_ACCESS" # 1회 액세스 후 기본 스토리지로 복원

# S3 설정 (프로덕션)
s3_enable_versioning = true # 프로덕션에서는 버전 관리 활성화
s3_kms_key_id        = null # 필요시 KMS 키 ID 설정

# Loki 로그 S3 라이프사이클 (프로덕션 - 장기 보관)
s3_loki_logs_ia_transition_days                    = 30   # 30일 후 Standard-IA
s3_loki_logs_glacier_transition_days               = 90   # 90일 후 Glacier
s3_loki_logs_deep_archive_transition_days          = 365  # 1년 후 Deep Archive
s3_loki_logs_expiration_days                       = 2555 # 7년 후 만료
s3_loki_logs_noncurrent_version_expiration_days    = 90   # 이전 버전 90일 후 만료

# 백업 S3 라이프사이클 (프로덕션 - 장기 보관)
s3_backups_ia_transition_days                      = 30   # 30일 후 Standard-IA
s3_backups_glacier_transition_days                 = 90   # 90일 후 Glacier
s3_backups_deep_archive_transition_days            = 180  # 6개월 후 Deep Archive
s3_backups_expiration_days                         = 3650 # 10년 후 만료
s3_backups_noncurrent_version_expiration_days      = 365  # 이전 버전 1년 후 만료

