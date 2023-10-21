FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["GlobalExceptionDemo/GlobalExceptionDemo.csproj", "GlobalExceptionDemo/"]
RUN dotnet restore "GlobalExceptionDemo/GlobalExceptionDemo.csproj"
COPY . .
WORKDIR "/src/GlobalExceptionDemo"
RUN dotnet build "GlobalExceptionDemo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GlobalExceptionDemo.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GlobalExceptionDemo.dll"]
