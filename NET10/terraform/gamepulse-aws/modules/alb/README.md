# ALB (Application Load Balancer) 모듈

이 모듈은 GamePulse 애플리케이션을 위한 Application Load Balancer를 생성하고 구성합니다.

## 기능

- Application Load Balancer 생성
- SSL/TLS 인증서 관리 (ACM)
- 타겟 그룹 및 헬스 체크 구성
- HTTP/HTTPS 리스너 설정
- 보안 그룹 구성
- 액세스 로그 (선택적)
- WAF 통합 (선택적)
- 세션 스티키니스 (선택적)
- 모니터링 서비스용 타겟 그룹 (선택적)

## 사용법

### 기본 사용법 (HTTP만)

```hcl
module "alb" {
  source = "./modules/alb"

  project_name         = "gamepulse"
  vpc_id              = module.vpc.vpc_id
  public_subnet_ids   = module.vpc.public_subnet_ids
  ecs_security_group_id = module.security_groups.ecs_security_group_id

  common_tags = {
    Environment = "production"
    Project     = "gamepulse"
  }
}
```

### HTTPS 사용법 (SSL 인증서 포함)

```hcl
module "alb" {
  source = "./modules/alb"

  project_name         = "gamepulse"
  vpc_id              = module.vpc.vpc_id
  public_subnet_ids   = module.vpc.public_subnet_ids
  ecs_security_group_id = module.security_groups.ecs_security_group_id

  # SSL 설정
  domain_name = "api.gamepulse.com"
  subject_alternative_names = ["www.api.gamepulse.com"]
  ssl_policy = "ELBSecurityPolicy-TLS-1-2-2017-01"

  # 헬스 체크 설정
  health_check_path = "/health"

  common_tags = {
    Environment = "production"
    Project     = "gamepulse"
  }
}
```

### 고급 설정 (액세스 로그, WAF, 스티키니스 포함)

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

  # 액세스 로그 설정
  enable_access_logs = true
  access_logs_bucket = "gamepulse-alb-logs"
  access_logs_prefix = "alb-logs"

  # WAF 설정
  enable_waf = true
  waf_acl_arn = aws_wafv2_web_acl.main.arn

  # 세션 스티키니스
  enable_stickiness = true
  stickiness_duration = 86400

  # 모니터링 타겟 그룹
  create_monitoring_target_groups = true

  # 고급 설정
  enable_deletion_protection = true
  enable_http2 = true
  idle_timeout = 60

  common_tags = {
    Environment = "production"
    Project     = "gamepulse"
  }
}
```

## 입력 변수

| 이름 | 설명 | 타입 | 기본값 | 필수 |
|------|------|------|--------|:----:|
| project_name | 프로젝트 이름 | `string` | n/a | yes |
| vpc_id | VPC ID | `string` | n/a | yes |
| public_subnet_ids | 퍼블릭 서브넷 ID 목록 | `list(string)` | n/a | yes |
| ecs_security_group_id | ECS 보안 그룹 ID | `string` | n/a | yes |
| domain_name | SSL 인증서용 도메인 이름 | `string` | `""` | no |
| subject_alternative_names | 추가 도메인 이름 목록 | `list(string)` | `[]` | no |
| health_check_path | 헬스 체크 경로 | `string` | `"/health"` | no |
| ssl_policy | SSL 보안 정책 | `string` | `"ELBSecurityPolicy-TLS-1-2-2017-01"` | no |
| enable_deletion_protection | 삭제 보호 활성화 | `bool` | `false` | no |
| enable_access_logs | 액세스 로그 활성화 | `bool` | `false` | no |
| access_logs_bucket | 액세스 로그 S3 버킷 | `string` | `""` | no |
| enable_waf | WAF 활성화 | `bool` | `false` | no |
| waf_acl_arn | WAF ACL ARN | `string` | `""` | no |
| enable_stickiness | 세션 스티키니스 활성화 | `bool` | `false` | no |
| common_tags | 공통 태그 | `map(string)` | `{}` | no |

## 출력값

| 이름 | 설명 |
|------|------|
| alb_arn | ALB ARN |
| alb_dns_name | ALB DNS 이름 |
| alb_zone_id | ALB Zone ID |
| alb_security_group_id | ALB 보안 그룹 ID |
| target_group_arn | 타겟 그룹 ARN |
| target_group_name | 타겟 그룹 이름 |
| ssl_certificate_arn | SSL 인증서 ARN |

## 보안 고려사항

1. **SSL/TLS**: 프로덕션 환경에서는 반드시 HTTPS를 사용하세요.
2. **보안 그룹**: 필요한 포트만 개방하도록 구성되어 있습니다.
3. **WAF**: 웹 애플리케이션 방화벽 사용을 권장합니다.
4. **액세스 로그**: 보안 모니터링을 위해 액세스 로그를 활성화하세요.

## 모니터링

- CloudWatch 메트릭이 자동으로 수집됩니다.
- 액세스 로그를 통해 트래픽 패턴을 분석할 수 있습니다.
- 헬스 체크를 통해 백엔드 서비스 상태를 모니터링합니다.

## 주의사항

1. SSL 인증서는 DNS 검증을 사용합니다. Route 53에서 도메인을 관리하는 경우 자동으로 검증됩니다.
2. 액세스 로그를 활성화하려면 S3 버킷이 미리 생성되어 있어야 합니다.
3. WAF를 사용하려면 WAF ACL이 미리 생성되어 있어야 합니다.
4. 삭제 보호가 활성화된 경우 Terraform으로 ALB를 삭제할 수 없습니다.

## 예제

더 많은 예제는 `examples/` 디렉토리를 참조하세요.