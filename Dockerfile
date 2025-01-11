FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER root
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AliCdnSSLWorker/AliCdnSSLWorker.csproj", "AliCdnSSLWorker"]
RUN dotnet restore "./AliCdnSSLWorker/AliCdnSSLWorker.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./AliCdnSSLWorker/AliCdnSSLWorker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AliCdnSSLWorker/AliCdnSSLWorker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AliCdnSSLWorker.dll"]
