# Database Reset Script for Railway
# This script drops and recreates the database to resolve migration conflicts

param(
    [string]$Host = $env:MYSQLHOST,
    [string]$Database = $env:MYSQLDATABASE,
    [string]$User = $env:MYSQLUSER,
    [string]$Password = $env:MYSQLPASSWORD,
    [string]$Port = $env:MYSQLPORT
)

# Validate required parameters
if (-not $Host -or -not $Database -or -not $User -or -not $Password) {
    Write-Error "Missing required database connection parameters. Please set MYSQLHOST, MYSQLDATABASE, MYSQLUSER, MYSQLPASSWORD environment variables."
    exit 1
}

$Port = if ($Port) { $Port } else { "3306" }

Write-Host "Resetting database $Database on $Host:$Port..." -ForegroundColor Red
Write-Host "WARNING: This will DELETE ALL DATA in the database!" -ForegroundColor Yellow

# Confirm action
$confirmation = Read-Host "Are you sure you want to continue? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Database reset cancelled." -ForegroundColor Green
    exit 0
}

try {
    Write-Host "Connecting to MySQL server..." -ForegroundColor Yellow
    
    # Drop the database
    Write-Host "Dropping database $Database..." -ForegroundColor Yellow
    $dropResult = & mysql -h $Host -P $Port -u $User -p$Password -e "DROP DATABASE IF EXISTS $Database;" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database dropped successfully." -ForegroundColor Green
    } else {
        Write-Error "Failed to drop database. Error: $dropResult"
        exit 1
    }
    
    # Recreate the database
    Write-Host "Creating database $Database..." -ForegroundColor Yellow
    $createResult = & mysql -h $Host -P $Port -u $User -p$Password -e "CREATE DATABASE $Database CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database created successfully." -ForegroundColor Green
    } else {
        Write-Error "Failed to create database. Error: $createResult"
        exit 1
    }
    
    Write-Host "Database reset completed successfully!" -ForegroundColor Green
    Write-Host "You can now redeploy your application and migrations will run cleanly." -ForegroundColor Cyan
    
} catch {
    Write-Error "Error during database reset: $_"
    exit 1
} 