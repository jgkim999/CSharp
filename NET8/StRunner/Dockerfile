# 디버그 컨테이너를 사용자 지정하는 방법과 Visual Studio 이 Dockerfile을 사용하여 더 빠른 디버깅을 위해 이미지를 빌드하는 방법을 알아보려면 https://aka.ms/customizecontainer를 참조하세요.

# 이 스테이지는 VS에서 빠른 모드로 실행할 때 사용됩니다(디버그 구성의 기본값).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
ENV TZ=Asia/Seoul
ENV K6_WEB_DASHBOARD=true
ENV K6_PROMETHEUS_RW_SERVER_URL=http://192.168.0.47:9090/api/v1/write
ENV K6_PROMETHEUS_RW_TREND_AS_NATIVE_HISTOGRAM=true
#ENV K6_PROMETHEUS_RW_TREND_STATS=p(95),p(99),min,max
WORKDIR /app
EXPOSE 8080
# k6 web dashboard
EXPOSE 5665
# k6 rest api
# https://grafana.com/docs/k6/latest/misc/k6-rest-api/
EXPOSE 6565

# 이 스테이지는 서비스 프로젝트를 빌드하는 데 사용됩니다.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["StRunner.csproj", "."]
RUN dotnet restore "./StRunner.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "/src/StRunner.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 이 스테이지는 최종 스테이지에 복사할 서비스 프로젝트를 게시하는 데 사용됩니다.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./StRunner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 이 스테이지는 프로덕션에서 사용되거나 VS에서 일반 모드로 실행할 때 사용됩니다(디버그 구성을 사용하지 않는 경우 기본값).
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StRunner.dll"]
