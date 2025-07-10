# PowerShell script to set a clean test environment and run dotnet test

Write-Host "Setting environment variables for .NET integration/unit tests..."

# Change to project root directory (where .sln file is located)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
Set-Location $projectRoot

Write-Host "Changed to project root: $projectRoot"

# Temporarily rename .env file to prevent it from overriding our test environment
$envFile = Join-Path $projectRoot ".env"
$envBackup = Join-Path $projectRoot ".env.backup"
$envFileExists = Test-Path $envFile

if ($envFileExists) {
    Write-Host "Temporarily renaming .env file to prevent environment variable conflicts..."
    Move-Item $envFile $envBackup
}

try {
    # Set environment variables for testing
    $env:ASPNETCORE_ENVIRONMENT = "Test"
    $env:STORAGE_PROVIDER = "memory"

    # Remove variables that could interfere
    Remove-Item Env:MYSQLHOST -ErrorAction SilentlyContinue
    Remove-Item Env:MYSQLDATABASE -ErrorAction SilentlyContinue
    Remove-Item Env:MYSQLUSER -ErrorAction SilentlyContinue
    Remove-Item Env:MYSQLPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:MYSQLPORT -ErrorAction SilentlyContinue
    Remove-Item Env:SFTP_HOST -ErrorAction SilentlyContinue
    Remove-Item Env:SFTP_USERNAME -ErrorAction SilentlyContinue
    Remove-Item Env:SFTP_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:SFTP_PRIVATE_KEY -ErrorAction SilentlyContinue
    Remove-Item Env:SFTP_PRIVATE_KEY_PATH -ErrorAction SilentlyContinue

    Write-Host "Test environment variables set:"
    Write-Host "  ASPNETCORE_ENVIRONMENT: $env:ASPNETCORE_ENVIRONMENT"
    Write-Host "  STORAGE_PROVIDER: $env:STORAGE_PROVIDER"
    Write-Host "  MYSQLHOST: $env:MYSQLHOST"
    Write-Host "  SFTP_HOST: $env:SFTP_HOST"

    Write-Host "Running: dotnet test $args"

    # Run dotnet test with any arguments passed to this script
    dotnet test @args
}
finally {
    # Restore .env file if it existed
    if ($envFileExists) {
        Write-Host "Restoring .env file..."
        Move-Item $envBackup $envFile
    }
} 