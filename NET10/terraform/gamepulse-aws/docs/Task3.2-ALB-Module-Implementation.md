# Task 3.2: Application Load Balancer Terraform 모듈 구현

## 개요

GamePulse 애플리케이션을 위한 Application Load Balancer (ALB) Terraform 모듈을 구현했습니다. 이 모듈은 ALB, 타겟 그룹, 리스너 규칙, SSL 인증서 및 보안 정책을 포함합니다.

## 구현된 기능

### 1. 핵심 ALB 리소스

- **Application Load Balancer**: 인터넷 대면 ALB 생성
- **보안 그룹**: ALB 전용 보안 그룹 (HTTP/HTTPS 인바운드, ECS 아웃바운드)
- **타겟 그룹**: GamePulse 애플리케이션용 타겟 그룹 (포트 8080)
- **헬스 체크**: 구성 가능한 헬스 체크 설정

### 2. SSL/TLS 지원

- **ACM 인증서**: 자동 SSL 인증서 생성 및 관리
- **DNS 검증**: Route 53을 통한 자동 도메인 검증
- **리스너 구성**: HTTP → HTTPS 리다이렉트 및 HTTPS 리스너
- **보안 정책**: 구성 가능한 SSL 보안 정책

### 3. 고급 기능

- **액세스 로그**: S3 버킷을 통한 ALB 액세스 로그
- **WAF 통합**: AWS WAF Web ACL 연결 지원
- **세션 스티키니스**: 쿠키 기반 세션 유지
- **모니터링 타겟 그룹**: Grafana 등 모니터링 서비스용 별도 타겟 그룹

### 4. 보안 및 성능 최적화

- **HTTP/2 지원**: 성능 향상을 위한 HTTP/2 활성화
- **크로스 존 로드 밸런싱**: 가용 영역 간 트래픽 분산
- **잘못된 헤더 필드 삭제**: 보안 강화
- **구성 가능한 타임아웃**: 유휴 타임아웃 설정

## 파일 구조

```
terraform/gamepulse-aws/modules/alb/
├── main.tf              # 메인 리소스 정의
├── variables.tf         # 입력 변수 정의
├── outputs.tf           # 출력값 정의
├── README.md           # 모듈 사용법 문서
└── examples/           # 사용 예제
    ├── basic.tf        # 기본 사용 예제
    └── production.tf   # 프로덕션 사용 예제
```

## 주요 리소스

### ALB 메인 리소스

```hcl
resource "aws_lb" "main" {
  name               = "${var.project_name}-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = var.public_subnet_ids

  enable_deletion_protection       = var.enable_deletion_protection
  enable_cross_zone_load_balancing = var.enable_cross_zone_load_balancing
  enable_http2                     = var.enable_http2
  idle_timeout                     = var.idle_timeout
  drop_invalid_header_fields       = var.drop_invalid_header_fields
}
```

### 보안 그룹

```hcl
resource "aws_security_group" "alb" {
  name_prefix = "${var.project_name}-alb-"
  vpc_id      = var.vpc_id

  # HTTP/HTTPS 인바운드
  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # ECS로의 아웃바운드
  egress {
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [var.ecs_security_group_id]
  }
}
```

### SSL 인증서

```hcl
resource "aws_acm_certificate" "main" {
  count = var.domain_name != "" ? 1 : 0

  domain_name       = var.domain_name
  validation_method = "DNS"
  subject_alternative_names = var.subject_alternative_names

  lifecycle {
    create_before_destroy = true
  }
}
```

## 사용법

### 기본 사용법 (HTTP만)

```hcl
module "alb" {
  source = "./modules/alb"

  project_name         = "gamepulse"
  vpc_id              = module.vpc.vpc_id
  public_subnet_ids   = module.vpc.public_subnet_ids
  ecs_security_group_id = module.security_groups.ecs_security_group_id

  health_check_path = "/health"

  common_tags = {
    Environment = "production"
    Project     = "gamepulse"
  }
}
```

### HTTPS 사용법

```hcl
module "alb" {
  source = "./modules/alb"

  project_name         = "gamepulse"
  vpc_id              = module.vpc.vpc_id
  public_subnet_ids   = module.vpc.public_subnet_ids
  ecs_security_group_id = module.security_groups.ecs_security_group_id

  # SSL 설정
  domain_name = "api.gamepulse.com"
  ssl_policy = "ELBSecurityPolicy-TLS-1-2-2017-01"

  # 고급 설정
  enable_deletion_protection = true
  enable_access_logs = true
  access_logs_bucket = "gamepulse-alb-logs"

  common_tags = {
    Environment = "production"
    Project     = "gamepulse"
  }
}
```

## 환경별 설정

### 프로덕션 환경

- SSL 인증서 활성화
- 액세스 로그 활성화
- WAF 연결 권장
- 삭제 보호 활성화
- 엄격한 헬스 체크 설정

### 스테이징 환경

- SSL 선택적 사용
- 액세스 로그 선택적
- 삭제 보호 비활성화
- 표준 헬스 체크 설정

## 보안 고려사항

1. **SSL/TLS**: 프로덕션에서는 반드시 HTTPS 사용
2. **보안 그룹**: 최소 권한 원칙 적용
3. **WAF**: 웹 애플리케이션 방화벽 사용 권장
4. **액세스 로그**: 보안 모니터링을 위한 로그 활성화

## 모니터링

- CloudWatch 메트릭 자동 수집
- 액세스 로그를 통한 트래픽 분석
- 헬스 체크를 통한 백엔드 상태 모니터링
- 타겟 그룹 메트릭 모니터링

## 출력값

주요 출력값들:

- `alb_dns_name`: ALB DNS 이름
- `alb_arn`: ALB ARN
- `target_group_arn`: 타겟 그룹 ARN
- `ssl_certificate_arn`: SSL 인증서 ARN
- `alb_security_group_id`: ALB 보안 그룹 ID

## 다음 단계

1. **ECS 서비스 연결**: ALB 타겟 그룹에 ECS 서비스 연결
2. **Route 53 설정**: 도메인 DNS 레코드 생성
3. **WAF 구성**: 웹 애플리케이션 방화벽 설정
4. **모니터링 설정**: CloudWatch 대시보드 및 알람 구성

## 주의사항

1. SSL 인증서는 DNS 검증을 사용하므로 Route 53에서 도메인 관리 필요
2. 액세스 로그 활성화 시 S3 버킷 사전 생성 필요
3. WAF 사용 시 WAF ACL 사전 생성 필요
4. 삭제 보호 활성화 시 Terraform으로 삭제 불가

## 트러블슈팅

### 일반적인 문제들

1. **SSL 인증서 검증 실패**: Route 53 DNS 레코드 확인
2. **헬스 체크 실패**: 애플리케이션 헬스 엔드포인트 확인
3. **보안 그룹 연결 오류**: ECS 보안 그룹 ID 확인
4. **타겟 등록 실패**: 서브넷 및 VPC 설정 확인

### 로그 확인

```bash
# ALB 액세스 로그 확인
aws s3 ls s3://your-alb-logs-bucket/alb-logs/

# CloudWatch 메트릭 확인
aws cloudwatch get-metric-statistics \
  --namespace AWS/ApplicationELB \
  --metric-name RequestCount \
  --dimensions Name=LoadBalancer,Value=app/gamepulse-alb/xxx \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-01T01:00:00Z \
  --period 300 \
  --statistics Sum
```

## 참고 자료

- [AWS Application Load Balancer 사용자 가이드](https://docs.aws.amazon.com/elasticloadbalancing/latest/application/)
- [Terraform AWS Provider - ALB](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/lb)
- [AWS ACM 사용자 가이드](https://docs.aws.amazon.com/acm/)
- [AWS WAF 개발자 가이드](https://docs.aws.amazon.com/waf/)