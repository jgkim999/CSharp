# Loki 로그 스토리지용 S3 버킷
resource "aws_s3_bucket" "loki_logs" {
  bucket = "${var.project_name}-${var.environment}-loki-logs-${random_id.bucket_suffix.hex}"
  
  tags = merge(var.tags, {
    Name    = "${var.project_name}-${var.environment}-loki-logs"
    Purpose = "Loki log storage"
  })
}

# 백업 및 아카이브용 S3 버킷
resource "aws_s3_bucket" "backups" {
  bucket = "${var.project_name}-${var.environment}-backups-${random_id.bucket_suffix.hex}"
  
  tags = merge(var.tags, {
    Name    = "${var.project_name}-${var.environment}-backups"
    Purpose = "Backup and archive storage"
  })
}

# 버킷 이름 고유성을 위한 랜덤 ID
resource "random_id" "bucket_suffix" {
  byte_length = 4
}

# Loki 로그 버킷 버전 관리
resource "aws_s3_bucket_versioning" "loki_logs" {
  bucket = aws_s3_bucket.loki_logs.id
  versioning_configuration {
    status = var.enable_versioning ? "Enabled" : "Suspended"
  }
}

# 백업 버킷 버전 관리
resource "aws_s3_bucket_versioning" "backups" {
  bucket = aws_s3_bucket.backups.id
  versioning_configuration {
    status = var.enable_versioning ? "Enabled" : "Suspended"
  }
}

# Loki 로그 버킷 암호화
resource "aws_s3_bucket_server_side_encryption_configuration" "loki_logs" {
  bucket = aws_s3_bucket.loki_logs.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm     = var.kms_key_id != null ? "aws:kms" : "AES256"
      kms_master_key_id = var.kms_key_id
    }
    bucket_key_enabled = var.kms_key_id != null ? true : false
  }
}

# 백업 버킷 암호화
resource "aws_s3_bucket_server_side_encryption_configuration" "backups" {
  bucket = aws_s3_bucket.backups.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm     = var.kms_key_id != null ? "aws:kms" : "AES256"
      kms_master_key_id = var.kms_key_id
    }
    bucket_key_enabled = var.kms_key_id != null ? true : false
  }
}

# Loki 로그 버킷 퍼블릭 액세스 차단
resource "aws_s3_bucket_public_access_block" "loki_logs" {
  bucket = aws_s3_bucket.loki_logs.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# 백업 버킷 퍼블릭 액세스 차단
resource "aws_s3_bucket_public_access_block" "backups" {
  bucket = aws_s3_bucket.backups.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# Loki 로그 버킷 라이프사이클 정책
resource "aws_s3_bucket_lifecycle_configuration" "loki_logs" {
  bucket = aws_s3_bucket.loki_logs.id

  rule {
    id     = "loki_logs_lifecycle"
    status = "Enabled"

    # 필터 추가 (모든 객체에 적용)
    filter {}

    # 현재 버전 객체 전환
    transition {
      days          = var.loki_logs_ia_transition_days
      storage_class = "STANDARD_IA"
    }

    transition {
      days          = var.loki_logs_glacier_transition_days
      storage_class = "GLACIER"
    }

    transition {
      days          = var.loki_logs_deep_archive_transition_days
      storage_class = "DEEP_ARCHIVE"
    }

    # 현재 버전 객체 만료
    expiration {
      days = var.loki_logs_expiration_days
    }

    # 이전 버전 객체 관리
    dynamic "noncurrent_version_transition" {
      for_each = var.enable_versioning ? [1] : []
      content {
        noncurrent_days = 30
        storage_class   = "STANDARD_IA"
      }
    }

    dynamic "noncurrent_version_expiration" {
      for_each = var.enable_versioning ? [1] : []
      content {
        noncurrent_days = var.loki_logs_noncurrent_version_expiration_days
      }
    }

    # 불완전한 멀티파트 업로드 정리
    abort_incomplete_multipart_upload {
      days_after_initiation = 7
    }
  }
}

# 백업 버킷 라이프사이클 정책
resource "aws_s3_bucket_lifecycle_configuration" "backups" {
  bucket = aws_s3_bucket.backups.id

  rule {
    id     = "backups_lifecycle"
    status = "Enabled"

    # 필터 추가 (모든 객체에 적용)
    filter {}

    # 현재 버전 객체 전환
    transition {
      days          = var.backups_ia_transition_days
      storage_class = "STANDARD_IA"
    }

    transition {
      days          = var.backups_glacier_transition_days
      storage_class = "GLACIER"
    }

    transition {
      days          = var.backups_deep_archive_transition_days
      storage_class = "DEEP_ARCHIVE"
    }

    # 현재 버전 객체 만료 (백업은 더 오래 보관)
    expiration {
      days = var.backups_expiration_days
    }

    # 이전 버전 객체 관리
    dynamic "noncurrent_version_transition" {
      for_each = var.enable_versioning ? [1] : []
      content {
        noncurrent_days = 90
        storage_class   = "GLACIER"
      }
    }

    dynamic "noncurrent_version_expiration" {
      for_each = var.enable_versioning ? [1] : []
      content {
        noncurrent_days = var.backups_noncurrent_version_expiration_days
      }
    }

    # 불완전한 멀티파트 업로드 정리
    abort_incomplete_multipart_upload {
      days_after_initiation = 7
    }
  }
}

# Loki 로그 버킷 알림 설정 (선택사항)
# 참고: S3 버킷 알림은 Lambda, SQS, SNS를 지원하며, CloudWatch는 직접 지원하지 않음
# CloudWatch 메트릭은 S3 자체에서 자동으로 제공됨