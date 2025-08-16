# GamePulse 최적화된 Dockerfile for AWS ECS 배포
# 멀티 스테이지 빌드를 통한 이미지 크기 최적화 및 보안 강화

# ============================================================================
# 1단계: 베이스 런타임 이미지 (요구사항 1.1, 1.2)
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# 보안 강화: 불필요한 패키지 제거 및 보안 업데이트
RUN apk update && \
    apk upgrade && \
    apk add --no-cache \
        curl \
        ca-certificates \
        tzdata && \
    rm -rf /var/cache/apk/*

# 시간대 설정 (한국 시간)
ENV TZ=Asia/Seoul

# ASP.NET Core 환경 변수 설정
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# OpenTelemetry 환경 변수 설정 (기본값, ECS에서 오버라이드 가능)
ENV OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
ENV OTEL_SERVICE_NAME=gamepulse
ENV OTEL_SERVICE_VERSION=1.0.0
ENV OTEL_SERVICE_NAMESPACE=production
ENV OTEL_DEPLOYMENT_ENVIRONMENT=aws
ENV OTEL_RESOURCE_ATTRIBUTES=service.version=1.0.0,deployment.environment=aws,container.runtime=docker

# 보안 강화: non-root 사용자 생성 (요구사항 1.2)
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# ============================================================================
# 2단계: 빌드 환경 (SDK 이미지)
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# 빌드 성능 최적화: 의존성 복원을 먼저 수행
COPY ["GamePulse.csproj", "./"]
RUN dotnet restore "GamePulse.csproj" \
    --runtime alpine-x64 \
    --no-cache

# 소스 코드 복사 및 빌드
COPY . .
RUN dotnet build "GamePulse.csproj" \
    -c Release \
    -o /app/build \
    --runtime alpine-x64 \
    --self-contained false \
    --no-restore

# ============================================================================
# 3단계: 게시 (Publish)
# ============================================================================
FROM build AS publish
RUN dotnet publish "GamePulse.csproj" \
    -c Release \
    -o /app/publish \
    --runtime alpine-x64 \
    --self-contained false \
    --no-restore \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false \
    /p:PublishReadyToRun=true

# ============================================================================
# 4단계: 최종 런타임 이미지
# ============================================================================
FROM base AS final
WORKDIR /app

# 게시된 애플리케이션 복사
COPY --from=publish --chown=appuser:appgroup /app/publish .

# 헬스체크 스크립트 생성
RUN echo '#!/bin/sh\ncurl -f http://localhost:8080/health || exit 1' > /app/healthcheck.sh && \
    chmod +x /app/healthcheck.sh && \
    chown appuser:appgroup /app/healthcheck.sh

# 애플리케이션 디렉토리 권한 설정
RUN chown -R appuser:appgroup /app && \
    chmod -R 755 /app

# 보안 강화: non-root 사용자로 실행 (요구사항 1.2)
USER appuser

# 헬스체크 설정
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD /app/healthcheck.sh

# 메타데이터 라벨 추가
LABEL maintainer="DevOps Team" \
      version="1.0.0" \
      description="GamePulse Application for AWS ECS" \
      project="GamePulse" \
      component="web-application"

# 애플리케이션 시작
ENTRYPOINT ["dotnet", "GamePulse.dll"]
