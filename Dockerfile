FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy DDD project files for better caching
COPY ["src/Normaize.DataNormalization.Domain/Normaize.DataNormalization.Domain.csproj", "src/Normaize.DataNormalization.Domain/"]
COPY ["src/Normaize.DataNormalization.Application/Normaize.DataNormalization.Application.csproj", "src/Normaize.DataNormalization.Application/"]
COPY ["src/Normaize.DataNormalization.Infrastructure/Normaize.DataNormalization.Infrastructure.csproj", "src/Normaize.DataNormalization.Infrastructure/"]
COPY ["src/Normaize.DataNormalization.API/Normaize.DataNormalization.API.csproj", "src/Normaize.DataNormalization.API/"]

# Restore dependencies
RUN dotnet restore "src/Normaize.DataNormalization.API/Normaize.DataNormalization.API.csproj"

# Copy everything else
COPY . .

# Build and publish
WORKDIR "/src/src/Normaize.DataNormalization.API"
RUN dotnet build "Normaize.DataNormalization.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Normaize.DataNormalization.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Railway provides PORT env var, but we use ASPNETCORE_URLS
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl --fail http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "Normaize.DataNormalization.API.dll"] 