FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for better caching
COPY ["Normaize.API/Normaize.API.csproj", "Normaize.API/"]
COPY ["Normaize.Core/Normaize.Core.csproj", "Normaize.Core/"]
COPY ["Normaize.Data/Normaize.Data.csproj", "Normaize.Data/"]

# Restore dependencies
RUN dotnet restore "Normaize.API/Normaize.API.csproj"

# Copy everything else
COPY . .

# Build and publish
WORKDIR "/src/Normaize.API"
RUN dotnet build "Normaize.API.csproj" -c Release -o /app/build
RUN dotnet publish "Normaize.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
  CMD ["curl", "--fail", "http://localhost:8080/api/healthmonitoring/readiness"]

ENTRYPOINT ["dotnet", "Normaize.API.dll"] 