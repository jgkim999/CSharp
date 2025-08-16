# ECR 모듈

GamePulse 프로젝트를 위한 AWS Elastic Container Registry (ECR) 리포지토리를 생성하고 관리하는 Terraform 모듈입니다.

## 기능

- GamePulse 애플리케이션용 ECR 리포지토리 생성
- OpenTelemetry Collector용 별도 ECR 리포지토리 생성 (선택사항)
- 이미지 스캔 및 취약점 검사 활성화
- 라이프사이클 정책을 통한 이미지 관리
- 암호화 설정 (AES256 또는 KMS)
- 적절한 태깅 및 명명 규칙 적용

## 요구사항 충족

이 모듈은 다음 요구사항을 충족합니다:

- **요구사항 1.4**: ECR 리포지토리 생성 시 이미지 스캔 및 라이프사이클 정책 설정

## 사용법

### 기본 사용법

```hcl
module "ecr" {
  source = "./modules/ecr"
  
  repository_name = "gamepulse"
  environment     = "prod"
  
  common_tags = {
    Project     = "GamePulse"
    Environment = "Production"
    ManagedBy   = "Terraform"
  }
}
```

### 고급 설정

```hcl
module "ecr" {
  source = "./modules/ecr"
  
  repository_name         = "gamepulse"
  environment            = "prod"
  image_tag_mutability   = "IMMUTABLE"
  scan_on_push          = true
  encryption_type       = "KMS"
  kms_key_id           = "arn:aws:kms:region:account:key/key-id"
  max_image_count      = 20
  untagged_image_days  = 3
  create_otel_repository = true
  
  common_tags = {
    Project     = "GamePulse"
    Environment = "Production"
    ManagedBy   = "Terraform"
    Owner       = "DevOps Team"
  }
}
```

## 입력 변수

| 변수명 | 설명 | 타입 | 기본값 | 필수 |
|--------|------|------|--------|------|
| `repository_name` | ECR 리포지토리 이름 | `string` | `"gamepulse"` | 아니오 |
| `environment` | 배포 환경 (dev, staging, prod) | `string` | - | 예 |
| `image_tag_mutability` | 이미지 태그 변경 가능성 | `string` | `"MUTABLE"` | 아니오 |
| `scan_on_push` | 이미지 푸시 시 자동 스캔 활성화 | `bool` | `true` | 아니오 |
| `encryption_type` | 암호화 타입 (AES256, KMS) | `string` | `"AES256"` | 아니오 |
| `kms_key_id` | KMS 키 ID | `string` | `null` | 아니오 |
| `force_delete` | 리포지토리 강제 삭제 허용 | `bool` | `false` | 아니오 |
| `max_image_count` | 유지할 최대 이미지 수 | `number` | `10` | 아니오 |
| `untagged_image_days` | 태그되지 않은 이미지 보관 일수 | `number` | `7` | 아니오 |
| `repository_policy` | ECR 리포지토리 정책 JSON | `string` | `null` | 아니오 |
| `create_otel_repository` | OpenTelemetry Collector 리포지토리 생성 여부 | `bool` | `true` | 아니오 |
| `common_tags` | 공통 태그 | `map(string)` | `{}` | 아니오 |

## 출력값

| 출력명 | 설명 |
|--------|------|
| `repository_url` | GamePulse ECR 리포지토리 URL |
| `repository_arn` | GamePulse ECR 리포지토리 ARN |
| `repository_name` | GamePulse ECR 리포지토리 이름 |
| `registry_id` | ECR 레지스트리 ID |
| `otel_collector_repository_url` | OpenTelemetry Collector ECR 리포지토리 URL |
| `docker_login_command` | ECR Docker 로그인 명령어 |
| `docker_build_and_push_commands` | Docker 빌드 및 푸시 명령어 예제 |

## 라이프사이클 정책

이 모듈은 다음과 같은 라이프사이클 정책을 적용합니다:

1. **버전 태그된 이미지**: 최대 `max_image_count`개의 이미지만 유지
2. **태그되지 않은 이미지**: `untagged_image_days`일 후 자동 삭제
3. **특수 태그**: `latest`, `main`, `develop` 태그는 각각 최신 1개만 유지

## 보안 기능

- **이미지 스캔**: 푸시 시 자동 취약점 스캔
- **암호화**: AES256 또는 KMS를 통한 저장 시 암호화
- **접근 제어**: IAM 정책을 통한 세밀한 권한 관리

## 예제

### Docker 이미지 빌드 및 푸시

```bash
# ECR 로그인
aws ecr get-login-password --region ap-northeast-2 | docker login --username AWS --password-stdin 123456789012.dkr.ecr.ap-northeast-2.amazonaws.com

# 이미지 빌드
docker build -t gamepulse .

# 이미지 태깅
docker tag gamepulse:latest 123456789012.dkr.ecr.ap-northeast-2.amazonaws.com/gamepulse:latest
docker tag gamepulse:latest 123456789012.dkr.ecr.ap-northeast-2.amazonaws.com/gamepulse:v1.0.0

# 이미지 푸시
docker push 123456789012.dkr.ecr.ap-northeast-2.amazonaws.com/gamepulse:latest
docker push 123456789012.dkr.ecr.ap-northeast-2.amazonaws.com/gamepulse:v1.0.0
```

## 모니터링

ECR 리포지토리는 다음과 같은 CloudWatch 메트릭을 제공합니다:

- `RepositoryPullCount`: 이미지 풀 횟수
- `RepositoryPushCount`: 이미지 푸시 횟수
- `RepositorySizeBytes`: 리포지토리 크기

## 문제 해결

### 일반적인 문제

1. **권한 오류**: ECR 접근을 위한 적절한 IAM 권한이 있는지 확인
2. **이미지 스캔 실패**: 이미지가 스캔 가능한 형식인지 확인
3. **라이프사이클 정책**: 정책이 예상대로 작동하지 않으면 CloudWatch 로그 확인

### 유용한 명령어

```bash
# ECR 리포지토리 목록 확인
aws ecr describe-repositories

# 이미지 목록 확인
aws ecr list-images --repository-name gamepulse

# 스캔 결과 확인
aws ecr describe-image-scan-findings --repository-name gamepulse --image-id imageTag=latest
```