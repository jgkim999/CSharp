# Task 2.3: ECR 푸시 스크립트 및 CI/CD 파이프라인 구현

## 개요

GamePulse 애플리케이션의 Docker 이미지를 AWS ECR에 자동으로 푸시하고, 시맨틱 버전 태깅 전략을 구현하며, GitHub Actions를 통한 완전한 CI/CD 파이프라인을 구성했습니다.

## 구현된 컴포넌트

### 1. ECR 푸시 자동화 스크립트

**파일**: `GamePulse/scripts/push-to-ecr.sh`

#### 주요 기능
- AWS ECR 자동 로그인 및 리포지토리 생성
- 시맨틱 버전 자동 증가 (major/minor/patch)
- 멀티 아키텍처 빌드 지원 (linux/amd64, linux/arm64)
- 이미지 태깅 전략 (버전, latest, Git 커밋, 날짜)
- ECR 이미지 스캔 자동 실행
- 드라이런 모드 지원

#### 사용법
```bash
# 기본 사용 (patch 버전 자동 증가)
./scripts/push-to-ecr.sh

# 특정 버전으로 푸시
./scripts/push-to-ecr.sh --version 1.2.3

# 멀티 아키텍처 빌드 및 latest 태그 포함
./scripts/push-to-ecr.sh --multi-arch --latest --scan

# 드라이런 모드
./scripts/push-to-ecr.sh --dry-run --version 1.2.3
```

#### 환경 변수
- `AWS_REGION`: AWS 리전 (기본값: ap-northeast-2)
- `AWS_ACCOUNT_ID`: AWS 계정 ID (자동 감지)
- `ECR_REPOSITORY`: ECR 리포지토리 이름 (기본값: gamepulse)

### 2. 시맨틱 버전 관리

**파일**: `GamePulse/version.txt`

#### 버전 관리 전략
- **Major**: 호환되지 않는 API 변경
- **Minor**: 하위 호환성을 유지하는 기능 추가
- **Patch**: 하위 호환성을 유지하는 버그 수정

#### 자동 증가 규칙
- `main` 브랜치: minor 버전 증가
- `develop` 브랜치: patch 버전 증가 + 브랜치명 접미사
- 릴리즈: 태그에서 버전 추출
- 수동 실행: 사용자 지정 버전

### 3. GitHub Actions CI/CD 파이프라인

**파일**: `.github/workflows/gamepulse-ci-cd.yml`

#### 파이프라인 단계

##### 1단계: 테스트 및 코드 품질 검사
- .NET 9 환경 설정
- 의존성 복원 및 빌드
- 단위 테스트 실행
- 코드 커버리지 수집
- CodeQL 보안 스캔

##### 2단계: Docker 빌드 및 ECR 푸시
- 버전 자동 결정
- Docker Buildx 설정
- ECR 로그인
- 멀티 아키텍처 빌드 (프로덕션)
- 이미지 취약점 스캔

##### 3단계: 스테이징 환경 배포
- ECS 태스크 정의 업데이트
- ECS 서비스 배포
- 배포 상태 확인

##### 4단계: 프로덕션 환경 배포
- 수동 승인 프로세스
- Blue/Green 배포 (CodeDeploy)
- 배포 후 헬스 체크
- 배포 완료 알림

##### 5단계: 통합 테스트
- k6 부하 테스트
- API 엔드포인트 검증
- 테스트 결과 아티팩트 저장

##### 6단계: 정리 작업
- 오래된 ECR 이미지 정리
- 실행 결과 요약

#### 트리거 조건
- `main`, `develop` 브랜치 푸시
- Pull Request (테스트만)
- 릴리즈 발행
- 수동 실행 (워크플로우 디스패치)

### 4. ECS 태스크 정의 템플릿

#### 스테이징 환경
**파일**: `terraform/gamepulse-aws/ecs-task-definition-staging.json`
- CPU: 512, Memory: 1024MB
- 환경: Staging
- 로그 그룹: `/ecs/gamepulse-staging`

#### 프로덕션 환경
**파일**: `terraform/gamepulse-aws/ecs-task-definition-production.json`
- CPU: 1024, Memory: 2048MB
- 환경: Production
- 로그 그룹: `/ecs/gamepulse-production`

#### 공통 구성
- GamePulse 애플리케이션 컨테이너
- OpenTelemetry Collector 사이드카
- AWS Secrets Manager 통합
- 헬스 체크 설정
- EFS 볼륨 마운트

### 5. CodeDeploy Blue/Green 배포

**파일**: `terraform/gamepulse-aws/appspec.yml`

#### 배포 훅 스크립트
1. **BeforeInstall**: 배포 전 상태 확인
2. **AfterInstall**: 새 태스크 정의 확인
3. **ApplicationStart**: 애플리케이션 시작 및 헬스 체크
4. **ApplicationStop**: 그레이스풀 셧다운
5. **BeforeAllowTraffic**: 트래픽 허용 전 검증
6. **AfterAllowTraffic**: 엔드투엔드 테스트

## 보안 고려사항

### 1. AWS 자격 증명
- GitHub Secrets를 통한 안전한 자격 증명 관리
- 최소 권한 원칙 적용
- IAM 역할 기반 접근 제어

### 2. 이미지 보안
- ECR 이미지 스캔 자동 실행
- 취약점 검사 결과 확인
- 베이스 이미지 정기 업데이트

### 3. 시크릿 관리
- AWS Secrets Manager 통합
- 환경별 시크릿 분리
- 컨테이너 런타임 시크릿 주입

## 모니터링 및 로깅

### 1. 배포 메트릭
- CloudWatch 커스텀 메트릭
- 배포 성공/실패 추적
- 성능 지표 수집

### 2. 로그 관리
- CloudWatch Logs 통합
- 구조화된 로그 포맷
- 로그 보존 정책

### 3. 알림
- SNS 토픽을 통한 배포 알림
- Slack/이메일 통합
- 에러 알림 자동화

## 사용 가이드

### 1. 초기 설정

#### GitHub Secrets 설정
```
AWS_ACCESS_KEY_ID: AWS 액세스 키
AWS_SECRET_ACCESS_KEY: AWS 시크릿 키
PRODUCTION_APPROVERS: 프로덕션 승인자 목록
```

#### AWS 리소스 준비
- ECR 리포지토리 생성 (자동)
- ECS 클러스터 및 서비스
- ALB 및 타겟 그룹
- IAM 역할 및 정책

### 2. 배포 프로세스

#### 개발 환경 배포
1. `develop` 브랜치에 코드 푸시
2. 자동으로 CI/CD 파이프라인 실행
3. 스테이징 환경에 자동 배포
4. 통합 테스트 실행

#### 프로덕션 배포
1. `main` 브랜치에 머지 또는 릴리즈 생성
2. 수동 승인 대기
3. Blue/Green 배포 실행
4. 배포 후 검증

### 3. 수동 배포

#### 특정 버전 배포
```bash
# GitHub Actions 수동 실행
# 워크플로우 디스패치에서 버전 및 환경 선택
```

#### 로컬 ECR 푸시
```bash
cd GamePulse
./scripts/push-to-ecr.sh --version 1.2.3 --latest --scan
```

## 트러블슈팅

### 1. 일반적인 문제

#### ECR 로그인 실패
```bash
# AWS 자격 증명 확인
aws sts get-caller-identity

# ECR 로그인 수동 실행
aws ecr get-login-password --region ap-northeast-2 | docker login --username AWS --password-stdin ACCOUNT_ID.dkr.ecr.ap-northeast-2.amazonaws.com
```

#### 이미지 빌드 실패
```bash
# Docker 상태 확인
docker info

# 빌드 로그 확인
docker build --no-cache -f Dockerfile .
```

#### 배포 실패
```bash
# ECS 서비스 상태 확인
aws ecs describe-services --cluster gamepulse-cluster --services gamepulse-service

# 태스크 로그 확인
aws logs get-log-events --log-group-name /ecs/gamepulse-production --log-stream-name STREAM_NAME
```

### 2. 성능 최적화

#### 빌드 시간 단축
- Docker 레이어 캐싱 활용
- 멀티 스테이지 빌드 최적화
- 의존성 복원 캐싱

#### 배포 시간 단축
- 헬스 체크 간격 조정
- 배포 훅 타임아웃 최적화
- 병렬 배포 고려

## 향후 개선사항

### 1. 고급 배포 전략
- Canary 배포 구현
- A/B 테스트 지원
- 자동 롤백 메커니즘

### 2. 보안 강화
- 이미지 서명 및 검증
- 런타임 보안 스캔
- 컴플라이언스 체크

### 3. 모니터링 확장
- 분산 트레이싱 통합
- 비즈니스 메트릭 수집
- 예측적 스케일링

## 결론

이번 구현을 통해 GamePulse 애플리케이션의 완전한 CI/CD 파이프라인을 구축했습니다. 시맨틱 버전 관리, 자동화된 ECR 푸시, Blue/Green 배포, 그리고 포괄적인 테스트 및 모니터링을 통해 안정적이고 확장 가능한 배포 프로세스를 제공합니다.

주요 성과:
- ✅ ECR 푸시 자동화 스크립트 구현
- ✅ 시맨틱 버전 태깅 전략 적용
- ✅ GitHub Actions CI/CD 파이프라인 구성
- ✅ Blue/Green 배포 지원
- ✅ 포괄적인 테스트 및 검증
- ✅ 보안 및 모니터링 통합

이제 개발팀은 코드 변경사항을 안전하고 효율적으로 프로덕션 환경에 배포할 수 있으며, 전체 배포 프로세스가 자동화되어 인적 오류를 최소화하고 배포 속도를 크게 향상시켰습니다.
