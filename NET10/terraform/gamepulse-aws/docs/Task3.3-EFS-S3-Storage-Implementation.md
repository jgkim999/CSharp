# Task 3.3: EFS 및 S3 스토리지 Terraform 모듈 구현

## 개요

GamePulse AWS 배포를 위한 EFS(Elastic File System) 및 S3(Simple Storage Service) 스토리지 Terraform 모듈을 구현했습니다. 이 모듈들은 모니터링 스택의 데이터 지속성과 로그 저장을 위한 스토리지 인프라를 제공합니다.

## 구현된 컴포넌트

### 1. EFS 모듈 (`modules/efs/`)

#### 주요 기능
- **EFS 파일 시스템**: 암호화된 공유 파일 시스템 생성
- **마운트 타겟**: 각 프라이빗 서브넷에 마운트 타겟 생성
- **액세스 포인트**: Prometheus, Jaeger, Grafana용 전용 액세스 포인트
- **보안 그룹**: EFS 접근을 위한 전용 보안 그룹
- **라이프사이클 정책**: 비용 최적화를 위한 IA 스토리지 전환

#### 생성되는 리소스
```hcl
# EFS 파일 시스템
resource "aws_efs_file_system" "main"

# 마운트 타겟 (각 프라이빗 서브넷)
resource "aws_efs_mount_target" "main"

# EFS 보안 그룹
resource "aws_security_group" "efs"

# 액세스 포인트
resource "aws_efs_access_point" "prometheus"
resource "aws_efs_access_point" "jaeger"
resource "aws_efs_access_point" "grafana"
```

#### 보안 설정
- **암호화**: 저장 시 암호화 활성화 (KMS 키 선택 가능)
- **네트워크 보안**: NFS 포트(2049)만 허용하는 전용 보안 그룹
- **액세스 제어**: 각 서비스별 전용 액세스 포인트와 POSIX 권한

### 2. S3 모듈 (`modules/s3/`)

#### 주요 기능
- **Loki 로그 버킷**: 로그 데이터 장기 저장용 S3 버킷
- **백업 버킷**: 시스템 백업 및 아카이브용 S3 버킷
- **라이프사이클 정책**: 비용 최적화를 위한 스토리지 클래스 전환
- **버전 관리**: 데이터 보호를 위한 객체 버전 관리
- **암호화**: 서버 측 암호화 (AES256 또는 KMS)

#### 생성되는 리소스
```hcl
# S3 버킷들
resource "aws_s3_bucket" "loki_logs"
resource "aws_s3_bucket" "backups"

# 버킷 설정
resource "aws_s3_bucket_versioning" "loki_logs"
resource "aws_s3_bucket_server_side_encryption_configuration" "loki_logs"
resource "aws_s3_bucket_public_access_block" "loki_logs"
resource "aws_s3_bucket_lifecycle_configuration" "loki_logs"

# 백업 버킷도 동일한 설정 적용
```

#### 라이프사이클 정책
- **Standard → Standard-IA**: 30일 후
- **Standard-IA → Glacier**: 90일 후
- **Glacier → Deep Archive**: 180일~365일 후
- **객체 만료**: 환경별 차등 적용 (스테이징: 1-2년, 프로덕션: 7-10년)

## 통합 구성

### 메인 Terraform 파일 업데이트

```hcl
# EFS 모듈 추가
module "efs" {
  source = "./modules/efs"
  
  project_name           = var.project_name
  environment            = var.environment
  vpc_id                 = module.vpc.vpc_id
  private_subnet_ids     = module.vpc.private_subnet_ids
  allowed_security_groups = [module.security_groups.ecs_security_group_id]
  
  # 성능 및 암호화 설정
  performance_mode = var.efs_performance_mode
  throughput_mode  = var.efs_throughput_mode
  kms_key_id       = var.efs_kms_key_id
}

# S3 모듈 추가
module "s3" {
  source = "./modules/s3"
  
  project_name      = var.project_name
  environment       = var.environment
  enable_versioning = var.s3_enable_versioning
  kms_key_id        = var.s3_kms_key_id
  
  # 라이프사이클 정책 설정
  # ... (환경별 설정)
}
```

### 변수 정의

#### EFS 관련 변수
- `efs_performance_mode`: 성능 모드 (generalPurpose/maxIO)
- `efs_throughput_mode`: 처리량 모드 (bursting/provisioned)
- `efs_kms_key_id`: 암호화 KMS 키 ID
- `efs_transition_to_ia`: IA 스토리지 전환 기간

#### S3 관련 변수
- `s3_enable_versioning`: 버전 관리 활성화
- `s3_kms_key_id`: 암호화 KMS 키 ID
- `s3_loki_logs_*`: Loki 로그 라이프사이클 설정
- `s3_backups_*`: 백업 라이프사이클 설정

### 환경별 설정

#### 프로덕션 환경
```hcl
# 장기 보관 정책
s3_loki_logs_expiration_days = 2555  # 7년
s3_backups_expiration_days   = 3650  # 10년

# 보안 강화
s3_enable_versioning = true
efs_performance_mode = "generalPurpose"
```

#### 스테이징 환경
```hcl
# 단기 보관 정책
s3_loki_logs_expiration_days = 365   # 1년
s3_backups_expiration_days   = 730   # 2년

# 개발 편의성
ecr_force_delete = true
```

## 출력값

### EFS 출력
- `efs_id`: 파일 시스템 ID
- `efs_dns_name`: 마운트용 DNS 이름
- `prometheus_access_point_id`: Prometheus 액세스 포인트 ID
- `jaeger_access_point_id`: Jaeger 액세스 포인트 ID
- `grafana_access_point_id`: Grafana 액세스 포인트 ID

### S3 출력
- `loki_logs_bucket_id`: Loki 로그 버킷 ID
- `loki_logs_bucket_arn`: Loki 로그 버킷 ARN
- `backups_bucket_id`: 백업 버킷 ID
- `backups_bucket_arn`: 백업 버킷 ARN

## 보안 고려사항

### EFS 보안
1. **네트워크 격리**: 프라이빗 서브넷에만 마운트 타겟 생성
2. **보안 그룹**: NFS 포트(2049)만 허용
3. **암호화**: 저장 시 암호화 활성화
4. **액세스 제어**: 서비스별 전용 액세스 포인트와 POSIX 권한

### S3 보안
1. **퍼블릭 액세스 차단**: 모든 퍼블릭 액세스 차단
2. **암호화**: 서버 측 암호화 (AES256 또는 KMS)
3. **버전 관리**: 실수로 인한 데이터 손실 방지
4. **IAM 정책**: 최소 권한 원칙 적용

## 비용 최적화

### EFS 비용 최적화
1. **IA 스토리지 클래스**: 30일 후 자동 전환
2. **버스팅 모드**: 일반적인 워크로드에 비용 효율적
3. **액세스 패턴 기반**: 1회 액세스 후 기본 스토리지로 복원

### S3 비용 최적화
1. **스토리지 클래스 전환**:
   - Standard → Standard-IA (30일)
   - Standard-IA → Glacier (90일)
   - Glacier → Deep Archive (180-365일)
2. **환경별 보존 정책**: 스테이징은 단기, 프로덕션은 장기
3. **불완전한 멀티파트 업로드 정리**: 7일 후 자동 정리

## 모니터링 스택 통합

### EFS 사용 계획
- **Prometheus**: `/prometheus` 경로에 메트릭 데이터 저장
- **Jaeger**: `/jaeger` 경로에 트레이스 데이터 저장
- **Grafana**: `/grafana` 경로에 대시보드 설정 저장

### S3 사용 계획
- **Loki**: 로그 데이터 장기 저장 백엔드
- **백업**: EFS 데이터 백업 및 설정 파일 아카이브

## 다음 단계

1. **OpenTelemetry Collector 구성**: EFS 마운트 설정 포함
2. **모니터링 스택 ECS 태스크**: EFS 볼륨 마운트 구성
3. **Loki 구성**: S3 백엔드 연결 설정
4. **백업 스크립트**: EFS 데이터의 S3 백업 자동화

## 검증 방법

### Terraform 검증
```bash
# 구성 검증
terraform validate

# 계획 확인
terraform plan -var-file=environments/prod.tfvars

# 적용
terraform apply -var-file=environments/prod.tfvars
```

### 리소스 확인
```bash
# EFS 파일 시스템 확인
aws efs describe-file-systems

# S3 버킷 확인
aws s3 ls

# 액세스 포인트 확인
aws efs describe-access-points
```

## 요구사항 충족

- ✅ **요구사항 4.4**: EFS 파일 시스템 및 마운트 타겟 생성
- ✅ **요구사항 5.3**: S3 버킷 및 라이프사이클 정책 구성
- ✅ **보안**: 암호화, 네트워크 격리, 액세스 제어
- ✅ **비용 최적화**: 스토리지 클래스 전환, 환경별 보존 정책
- ✅ **확장성**: 모듈화된 구조로 재사용 가능

이로써 Task 3.3 "EFS 및 S3 스토리지 Terraform 모듈 생성"이 완료되었습니다.