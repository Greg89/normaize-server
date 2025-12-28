# Docker Testing Guide

This guide helps you test the Normaize API locally using Docker before deploying to Railway.

## Quick Start

### 1. Start in Development Mode
```powershell
.\test-docker.ps1
```

This will:
- Start PostgreSQL and Redis
- Build and run the API in Development mode
- Enable Swagger UI at http://localhost:8080
- Run a quick health check

### 2. Test the Endpoints
```powershell
.\test-endpoints.ps1
```

This will test all major endpoints and give you a summary.

### 3. View Logs
```powershell
.\test-docker.ps1 -Logs
```

### 4. Stop Everything
```powershell
.\test-docker.ps1 -Down
```

## Testing Different Environments

### Test Beta Environment (Simulates Railway)
```powershell
.\test-docker.ps1 -Environment beta -Build
```

This uses the same configuration as your Railway Beta environment.

### Test Production Environment
```powershell
.\test-docker.ps1 -Environment production -Build
```

## Rebuilding After Code Changes

```powershell
.\test-docker.ps1 -Build
```

Or force a clean rebuild:
```powershell
.\test-docker.ps1 -Clean
.\test-docker.ps1 -Build
```

## Useful Commands

### View all running containers
```powershell
docker-compose ps
```

### View API logs in real-time
```powershell
docker-compose logs -f normaize-api
```

### Access PostgreSQL database
```powershell
docker exec -it normaize-postgres psql -U normaize_user -d normaize
```

### Access Redis CLI
```powershell
docker exec -it normaize-redis redis-cli
```

### Restart just the API
```powershell
docker-compose restart normaize-api
```

## Endpoints to Test

Once running, you can access:

- **Swagger UI**: http://localhost:8080
- **Health Check**: http://localhost:8080/health
- **Ready Check**: http://localhost:8080/health/ready
- **Live Check**: http://localhost:8080/health/live

## Troubleshooting

### Container won't start
```powershell
# Check logs
.\test-docker.ps1 -Logs

# Or view all service logs
docker-compose logs
```

### Database connection issues
```powershell
# Check if PostgreSQL is running
docker-compose ps postgres

# View PostgreSQL logs
docker-compose logs postgres
```

### Clean slate
```powershell
# Remove everything and start fresh
.\test-docker.ps1 -Clean
.\test-docker.ps1 -Build
```

## Environment Variables

The docker-compose setup uses these environment variables:

### Development (`docker-compose.override.yml`)
- `ASPNETCORE_ENVIRONMENT=Development`
- `Features__EnableSwagger=true`
- Detailed logging enabled

### Beta (`docker-compose.beta.yml`)
- `ASPNETCORE_ENVIRONMENT=Beta`
- `Features__EnableSwagger=true`
- Matches Railway Beta configuration

### Production (`docker-compose.yml` only)
- `ASPNETCORE_ENVIRONMENT=Production`
- Minimal logging
- Swagger disabled by default

## CI Pipeline Testing

Before pushing to GitHub, test locally:

1. **Test the build**:
   ```powershell
   dotnet build
   ```

2. **Test in Docker** (simulates CI):
   ```powershell
   .\test-docker.ps1 -Environment beta -Build
   ```

3. **Run endpoint tests**:
   ```powershell
   .\test-endpoints.ps1
   ```

4. **Check logs for errors**:
   ```powershell
   .\test-docker.ps1 -Logs
   ```

If all these pass, your CI pipeline should also pass! 🎉
