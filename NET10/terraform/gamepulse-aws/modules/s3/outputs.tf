output "loki_logs_bucket_id" {
  description = "Loki 로그 S3 버킷 ID"
  value       = aws_s3_bucket.loki_logs.id
}

output "loki_logs_bucket_arn" {
  description = "Loki 로그 S3 버킷 ARN"
  value       = aws_s3_bucket.loki_logs.arn
}

output "loki_logs_bucket_domain_name" {
  description = "Loki 로그 S3 버킷 도메인 이름"
  value       = aws_s3_bucket.loki_logs.bucket_domain_name
}

output "loki_logs_bucket_regional_domain_name" {
  description = "Loki 로그 S3 버킷 리전별 도메인 이름"
  value       = aws_s3_bucket.loki_logs.bucket_regional_domain_name
}

output "backups_bucket_id" {
  description = "백업 S3 버킷 ID"
  value       = aws_s3_bucket.backups.id
}

output "backups_bucket_arn" {
  description = "백업 S3 버킷 ARN"
  value       = aws_s3_bucket.backups.arn
}

output "backups_bucket_domain_name" {
  description = "백업 S3 버킷 도메인 이름"
  value       = aws_s3_bucket.backups.bucket_domain_name
}

output "backups_bucket_regional_domain_name" {
  description = "백업 S3 버킷 리전별 도메인 이름"
  value       = aws_s3_bucket.backups.bucket_regional_domain_name
}

output "bucket_suffix" {
  description = "버킷 이름에 사용된 랜덤 접미사"
  value       = random_id.bucket_suffix.hex
}

# 버킷 정보를 하나의 객체로 출력 (편의성을 위해)
output "buckets" {
  description = "모든 S3 버킷 정보"
  value = {
    loki_logs = {
      id                         = aws_s3_bucket.loki_logs.id
      arn                        = aws_s3_bucket.loki_logs.arn
      domain_name                = aws_s3_bucket.loki_logs.bucket_domain_name
      regional_domain_name       = aws_s3_bucket.loki_logs.bucket_regional_domain_name
    }
    backups = {
      id                         = aws_s3_bucket.backups.id
      arn                        = aws_s3_bucket.backups.arn
      domain_name                = aws_s3_bucket.backups.bucket_domain_name
      regional_domain_name       = aws_s3_bucket.backups.bucket_regional_domain_name
    }
  }
}