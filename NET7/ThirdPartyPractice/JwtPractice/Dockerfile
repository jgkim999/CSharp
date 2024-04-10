FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ThirdPartyPractice/JwtPractice/JwtPractice.csproj", "ThirdPartyPractice/JwtPractice/"]
RUN dotnet restore "ThirdPartyPractice/JwtPractice/JwtPractice.csproj"
COPY . .
WORKDIR "/src/ThirdPartyPractice/JwtPractice"
RUN dotnet build "JwtPractice.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JwtPractice.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JwtPractice.dll"]
