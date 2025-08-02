# Smart test runner that adapts based on context
param(
    [string]$Mode = "auto",  # auto, fast, integration, all, slow
    [switch]$Watch,
    [switch]$Verbose
)

Write-Host "Normaize Test Runner" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan

# Determine mode automatically if not specified
if ($Mode -eq "auto") {
    # Check if we're in CI/CD environment
    if ($env:CI -eq "true" -or $env:BUILD_ID) {
        $Mode = "all"
        Write-Host "CI/CD detected - running all tests" -ForegroundColor Yellow
    }
    # Check if we have uncommitted changes (git status)
    elseif (git status --porcelain) {
        $Mode = "fast"
        Write-Host "Uncommitted changes detected - running fast tests only" -ForegroundColor Yellow
    }
    else {
        $Mode = "integration"
        Write-Host "Clean working directory - running integration tests" -ForegroundColor Yellow
    }
}

$verbosity = if ($Verbose) { "normal" } else { "minimal" }
$logger = if ($Verbose) { "console;verbosity=normal" } else { "console;verbosity=minimal" }

switch ($Mode.ToLower()) {
    "fast" {
        Write-Host "Running fast unit tests..." -ForegroundColor Green
        $filter = "Category!=Integration&Category!=Slow&Category!=External&Category!=Database"
        dotnet test --filter $filter --logger $logger --verbosity $verbosity
    }
    "integration" {
        Write-Host "Running integration tests..." -ForegroundColor Yellow
        $filter = "Category=Integration"
        dotnet test --filter $filter --logger $logger --verbosity $verbosity
    }
    "slow" {
        Write-Host "Running slow tests..." -ForegroundColor Red
        $filter = "Category=Slow"
        dotnet test --filter $filter --logger $logger --verbosity $verbosity
    }
    "all" {
        Write-Host "Running all tests..." -ForegroundColor Cyan
        dotnet test --logger $logger --verbosity $verbosity --configuration Release
    }
    default {
        Write-Host "Unknown mode: $Mode" -ForegroundColor Red
        Write-Host "Available modes: fast, integration, slow, all, auto" -ForegroundColor Yellow
        exit 1
    }
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "Tests completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
} 