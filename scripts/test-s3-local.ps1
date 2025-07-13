# Test S3 Storage Locally
Write-Host "Testing S3 Storage Locally" -ForegroundColor Green
Write-Host "===========================" -ForegroundColor Green

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

Write-Host "Project root: $projectRoot" -ForegroundColor Cyan

# Test configuration logic first
Write-Host "`nTesting configuration logic..." -ForegroundColor Yellow
& "$scriptDir\test-s3-config.ps1"

Write-Host "`nChoose a test option:" -ForegroundColor Yellow
Write-Host "1. Test with Memory Storage (default)" -ForegroundColor White
Write-Host "2. Test with S3 (requires credentials)" -ForegroundColor White
Write-Host "3. Test with Local MinIO (requires MinIO running)" -ForegroundColor White
Write-Host "4. Test with Railway MinIO (requires Railway MinIO service)" -ForegroundColor White

$choice = Read-Host "`nEnter your choice (1-4)"

# Set environment variables based on choice
switch ($choice) {
    "1" {
        Write-Host "`nTesting with Memory Storage..." -ForegroundColor Green
        $env:STORAGE_PROVIDER = "memory"
        Remove-Item Env:AWS_ACCESS_KEY_ID -ErrorAction SilentlyContinue
        Remove-Item Env:AWS_SECRET_ACCESS_KEY -ErrorAction SilentlyContinue
        Remove-Item Env:AWS_REGION -ErrorAction SilentlyContinue
        Remove-Item Env:AWS_S3_BUCKET -ErrorAction SilentlyContinue
        Remove-Item Env:AWS_SERVICE_URL -ErrorAction SilentlyContinue
    }
    "2" {
        Write-Host "`nTesting with S3..." -ForegroundColor Green
        Write-Host "Please enter your AWS credentials:" -ForegroundColor Yellow
        $env:STORAGE_PROVIDER = "s3"
        $env:AWS_ACCESS_KEY_ID = Read-Host "AWS Access Key ID"
        $env:AWS_SECRET_ACCESS_KEY = Read-Host "AWS Secret Access Key"
        $env:AWS_REGION = Read-Host "AWS Region (default: us-east-1)" -Default "us-east-1"
        $env:AWS_S3_BUCKET = Read-Host "S3 Bucket Name (default: normaize-test-uploads)" -Default "normaize-test-uploads"
        Remove-Item Env:AWS_SERVICE_URL -ErrorAction SilentlyContinue
    }
    "3" {
        Write-Host "`nTesting with Local MinIO..." -ForegroundColor Green
        Write-Host "Make sure MinIO is running on localhost:9000" -ForegroundColor Yellow
        $env:STORAGE_PROVIDER = "s3"
        $env:AWS_ACCESS_KEY_ID = "minioadmin"
        $env:AWS_SECRET_ACCESS_KEY = "minioadmin"
        $env:AWS_REGION = "us-east-1"
        $env:AWS_S3_BUCKET = "normaize-test-uploads"
        $env:AWS_SERVICE_URL = "http://localhost:9000"
    }
    "4" {
        Write-Host "`nTesting with Railway MinIO..." -ForegroundColor Green
        Write-Host "Please enter your Railway MinIO credentials:" -ForegroundColor Yellow
        $env:STORAGE_PROVIDER = "s3"
        $env:AWS_ACCESS_KEY_ID = Read-Host "MinIO Access Key"
        $env:AWS_SECRET_ACCESS_KEY = Read-Host "MinIO Secret Key"
        $env:AWS_REGION = "us-east-1"
        $env:AWS_S3_BUCKET = Read-Host "MinIO Bucket Name (default: normaize-test-uploads)" -Default "normaize-test-uploads"
        $env:AWS_SERVICE_URL = Read-Host "MinIO Endpoint URL (e.g., https://minio-xxx.up.railway.app)"
    }
    default {
        Write-Host "Invalid choice. Using Memory Storage." -ForegroundColor Red
        $env:STORAGE_PROVIDER = "memory"
    }
}

# Display current configuration
Write-Host "`nCurrent Configuration:" -ForegroundColor Cyan
Write-Host "  STORAGE_PROVIDER: $env:STORAGE_PROVIDER" -ForegroundColor White
if ($env:STORAGE_PROVIDER -eq "s3") {
    Write-Host "  AWS_ACCESS_KEY_ID: $($env:AWS_ACCESS_KEY_ID.Substring(0, [Math]::Min(8, $env:AWS_ACCESS_KEY_ID.Length)))..." -ForegroundColor White
    Write-Host "  AWS_REGION: $env:AWS_REGION" -ForegroundColor White
    Write-Host "  AWS_S3_BUCKET: $env:AWS_S3_BUCKET" -ForegroundColor White
    if ($env:AWS_SERVICE_URL) {
        Write-Host "  AWS_SERVICE_URL: $env:AWS_SERVICE_URL" -ForegroundColor White
    }
}

# Change to project root
Set-Location $projectRoot

Write-Host "`nStarting application..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow

# Run the application
dotnet run --project Normaize.API 