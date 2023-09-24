FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ConsumerDemo1/ConsumerDemo1.csproj", "ConsumerDemo1/"]
RUN dotnet restore "ConsumerDemo1/ConsumerDemo1.csproj"
COPY . .
WORKDIR "/src/ConsumerDemo1"
RUN dotnet build "ConsumerDemo1.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsumerDemo1.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsumerDemo1.dll"]
