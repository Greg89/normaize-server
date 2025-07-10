# Database Schema Fix Script
# This script fixes missing columns and tables that should exist based on the current model
# Run this script if you encounter "Unknown column" errors after deployment

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

Write-Host "Fixing database schema for $Database on $Host:$Port..." -ForegroundColor Green

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFile = Join-Path $scriptDir "fix-missing-columns.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Error "SQL script not found: $sqlFile"
    exit 1
}

# Read the SQL script
$sqlContent = Get-Content $sqlFile -Raw

# Create temporary file with connection parameters
$tempSqlFile = [System.IO.Path]::GetTempFileName()
$connectionString = "mysql -h $Host -P $Port -u $User -p$Password $Database"

# Write the SQL content to temp file
$sqlContent | Out-File -FilePath $tempSqlFile -Encoding UTF8

try {
    Write-Host "Executing database schema fixes..." -ForegroundColor Yellow
    
    # Execute the SQL script
    $result = & mysql -h $Host -P $Port -u $User -p$Password $Database -e "source $tempSqlFile" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database schema fixes applied successfully!" -ForegroundColor Green
        Write-Host "Output:" -ForegroundColor Cyan
        Write-Host $result
    } else {
        Write-Error "Failed to apply database schema fixes. Error: $result"
        exit 1
    }
}
catch {
    Write-Error "Error executing database schema fixes: $_"
    exit 1
}
finally {
    # Clean up temporary file
    if (Test-Path $tempSqlFile) {
        Remove-Item $tempSqlFile -Force
    }
}

Write-Host "Database schema fix completed successfully!" -ForegroundColor Green 