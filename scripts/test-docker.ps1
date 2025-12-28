# Local Docker Testing Script for Normaize Server
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('dev', 'beta', 'production')]
    [string]$Environment = 'dev',
    [Parameter(Mandatory=$false)]
    [switch]$Build,
    [Parameter(Mandatory=$false)]
    [switch]$Down,
    [Parameter(Mandatory=$false)]
    [switch]$Logs,
    [Parameter(Mandatory=$false)]
    [switch]$Clean
)

Write-Host "Normaize Docker Testing Tool" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan
Write-Host ""

if ($Clean) {
    Write-Host "Cleaning up Docker resources..." -ForegroundColor Yellow
    docker-compose down -v
    docker system prune -f
    Write-Host "Cleanup complete!" -ForegroundColor Green
    exit 0
}

if ($Down) {
    Write-Host "Stopping containers..." -ForegroundColor Yellow
    docker-compose down
    Write-Host "Containers stopped!" -ForegroundColor Green
    exit 0
}

if ($Logs) {
    Write-Host "Showing logs (Ctrl+C to exit)..." -ForegroundColor Yellow
    docker-compose logs -f normaize-api
    exit 0
}

$composeFiles = @("docker-compose.yml")

if ($Environment -eq 'dev') {
    Write-Host "Starting in DEVELOPMENT mode" -ForegroundColor Green
    $composeFiles += "docker-compose.override.yml"
} elseif ($Environment -eq 'beta') {
    Write-Host "Starting in BETA mode (simulates Railway)" -ForegroundColor Yellow
    $composeFiles += "docker-compose.beta.yml"
} else {
    Write-Host "Starting in PRODUCTION mode" -ForegroundColor Red
}

$composeCmd = "docker-compose"
foreach ($file in $composeFiles) {
    $composeCmd += " -f $file"
}

if ($Build) {
    Write-Host ""
    Write-Host "Building Docker image..." -ForegroundColor Cyan
    Invoke-Expression "$composeCmd build --no-cache normaize-api"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build complete!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Starting services..." -ForegroundColor Cyan
Invoke-Expression "$composeCmd up -d"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start services!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "Service Status:" -ForegroundColor Cyan
docker-compose ps

Write-Host ""
Write-Host "Services are starting!" -ForegroundColor Green
Write-Host ""
Write-Host "API URL: http://localhost:8080" -ForegroundColor White
Write-Host "Swagger UI: http://localhost:8080" -ForegroundColor White
Write-Host "Health Check: http://localhost:8080/health" -ForegroundColor White
Write-Host ""
Write-Host "Useful commands:" -ForegroundColor Cyan
Write-Host "  .\test-docker.ps1 -Logs          # View API logs" -ForegroundColor Gray
Write-Host "  .\test-docker.ps1 -Down          # Stop services" -ForegroundColor Gray
Write-Host "  .\test-docker.ps1 -Clean         # Clean up everything" -ForegroundColor Gray
Write-Host ""
Write-Host "Testing health endpoint in 10 seconds..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -Method Get -TimeoutSec 5
    Write-Host "Health check passed!" -ForegroundColor Green
    Write-Host $response.Content
} catch {
    Write-Host "Health check failed (service may still be starting):" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Gray
    Write-Host ""
    Write-Host "Run '.\test-docker.ps1 -Logs' to see what's happening" -ForegroundColor Cyan
}
