FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Prometheus/Prometheus.csproj", "Prometheus/"]
RUN dotnet restore "Prometheus/Prometheus.csproj"
COPY . .
WORKDIR "/src/Prometheus"
RUN dotnet build "Prometheus.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Prometheus.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Prometheus.dll"]
