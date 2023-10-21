FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["EF/EfDemo1/EfDemo1.csproj", "EF/EfDemo1/"]
RUN dotnet restore "EF/EfDemo1/EfDemo1.csproj"
COPY . .
WORKDIR "/src/EF/EfDemo1"
RUN dotnet build "EfDemo1.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EfDemo1.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EfDemo1.dll"]
