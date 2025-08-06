# GamePulse ECS Deployment with Terraform

이 Terraform 구성은 GamePulse .NET 9.0 애플리케이션을 AWS ECS Fargate에 배포합니다.

## 아키텍처

- **VPC**: 퍼블릭/프라이빗 서브넷을 가진 사용자 정의 VPC
- **ECS Fargate**: 컨테이너화된 애플리케이션 실행
- **Application Load Balancer**: 트래픽 분산 및 헬스 체크
- **ECR**: Docker 이미지 저장소
- **Auto Scaling**: CPU, 메모리, 요청 수 기반 자동 확장
- **CloudWatch**: 로그 및 모니터링

## 사전 요구사항

1. **AWS CLI 설치 및 구성**

   ```bash
   aws configure
   ```

2. **Terraform 설치**

   ```bash
   # macOS
   brew install terraform
   ```

3. **Docker 설치** (이미지 빌드용)

## 배포 단계

### 1. 변수 설정

```bash
cp terraform.tfvars.example terraform.tfvars
# terraform.tfvars 파일을 편집하여 필요한 값들을 설정
```

### 2. Terraform 초기화

```bash
terraform init
```

### 3. 계획 확인

```bash
terraform plan
```

### 4. 인프라 배포

```bash
terraform apply
```

### 5. Docker 이미지 빌드 및 푸시

배포 완료 후 ECR 리포지토리 URL을 확인:

```bash
terraform output ecr_repository_url
```

GamePulse 디렉토리에서 Docker 이미지 빌드:

```bash
cd ../GamePulse

# Dockerfile 생성 (아래 참조)
# Docker 이미지 빌드
docker build -t gamepulse .

# ECR 로그인
aws ecr get-login-password --region ap-northeast-2 | docker login --username AWS --password-stdin <ECR_REPOSITORY_URL>

# 이미지 태그 및 푸시
docker tag gamepulse:latest <ECR_REPOSITORY_URL>:latest
docker push <ECR_REPOSITORY_URL>:latest
```

### 6. ECS 서비스 업데이트

```bash
# 새 이미지로 서비스 업데이트
aws ecs update-service --cluster gamepulse-cluster --service gamepulse-service --force-new-deployment
```

## GamePulse Dockerfile

GamePulse 디렉토리에 다음 Dockerfile을 생성하세요:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["GamePulse.csproj", "."]
RUN dotnet restore "./GamePulse.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "GamePulse.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GamePulse.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GamePulse.dll"]
```

## 헬스 체크 엔드포인트

GamePulse 애플리케이션에 `/health` 엔드포인트를 추가하세요:

```csharp
// Program.cs에 추가
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
```

## 모니터링

- **CloudWatch Logs**: `/ecs/gamepulse` 로그 그룹에서 애플리케이션 로그 확인
- **ECS Console**: 서비스 상태 및 태스크 모니터링
- **ALB**: 헬스 체크 및 트래픽 분산 상태 확인

## 정리

리소스 삭제:

```bash
terraform destroy
```

## 주요 출력값

- `application_url`: 애플리케이션 접근 URL
- `ecr_repository_url`: ECR 리포지토리 URL
- `load_balancer_dns_name`: 로드 밸런서 DNS 이름

## 비용 최적화

- Fargate Spot 인스턴스 사용 (기본 설정)
- CloudWatch 로그 보존 기간: 7일
- Auto Scaling으로 필요시에만 확장

## 보안 고려사항

- 프라이빗 서브넷에서 ECS 태스크 실행
- 보안 그룹으로 네트워크 접근 제한
- ECR 이미지 스캔 활성화
- IAM 역할 최소 권한 원칙 적용

## Scaling

```text
cpu_target_value: CPU 사용률 임계값 (기본값: 70.0%)
memory_target_value: 메모리 사용률 임계값 (기본값: 80.0%)
request_count_target_value: 타겟당 요청 수 임계값 (기본값: 1000.0)
scale_in_cooldown: 스케일 인 쿨다운 시간 (기본값: 300초)
scale_out_cooldown: 스케일 아웃 쿨다운 시간 (기본값: 300초)
autoscaling.tf 업데이트:
```

## VPC

```test
VPC (10.0.0.0/20) - 4,096 IPs
├── Public Subnets (ALB, NAT Gateway)
│   ├── AZ-1a: 10.0.1.0/26 (64 IPs)
│   ├── AZ-1b: 10.0.1.64/26 (64 IPs)
│   └── AZ-1c: 10.0.1.128/26 (64 IPs)
├── Private Subnets (ECS Tasks)
│   ├── AZ-1a: 10.0.2.0/26 (64 IPs)
│   ├── AZ-1b: 10.0.2.64/26 (64 IPs)
│   └── AZ-1c: 10.0.2.128/26 (64 IPs)
└── Database Subnets (RDS, Redis)
    ├── AZ-1a: 10.0.3.0/26 (64 IPs)
    ├── AZ-1b: 10.0.3.64/26 (64 IPs)
    └── AZ-1c: 10.0.3.128/26 (64 IPs)
```
