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

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Copy project files needed for migrations to a separate directory
COPY --from=build /src/Normaize.Data ./src/Normaize.Data/
COPY --from=build /src/Normaize.API ./src/Normaize.API/

# Copy migration files to the container
COPY --from=build /src/Normaize.Data/Migrations ./Migrations/

# Install EF Core tools locally and add to PATH
RUN dotnet tool install --tool-path /tools dotnet-ef
ENV PATH="/tools:${PATH}"

# Copy and set up startup script
COPY scripts/startup.sh ./startup.sh
RUN chmod +x ./startup.sh

HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:8080/health/readiness || exit 1

ENTRYPOINT ["./startup.sh"] 