# Test S3 Configuration Logic
Write-Host "Testing S3 Configuration Logic" -ForegroundColor Green
Write-Host "===============================" -ForegroundColor Green

# Test 1: No STORAGE_PROVIDER set (should default to memory)
Write-Host "`nTest 1: No STORAGE_PROVIDER set" -ForegroundColor Yellow
$env:STORAGE_PROVIDER = $null
$env:AWS_ACCESS_KEY_ID = $null
$env:AWS_SECRET_ACCESS_KEY = $null

$storageProvider = $env:STORAGE_PROVIDER
if ([string]::IsNullOrEmpty($storageProvider)) {
    $storageProvider = "memory"
    Write-Host "  Defaulting to: $storageProvider" -ForegroundColor Green
}

# Test 2: STORAGE_PROVIDER=s3 but no credentials (should fallback to memory)
Write-Host "`nTest 2: STORAGE_PROVIDER=s3 but no credentials" -ForegroundColor Yellow
$env:STORAGE_PROVIDER = "s3"
$env:AWS_ACCESS_KEY_ID = $null
$env:AWS_SECRET_ACCESS_KEY = $null

$storageProvider = $env:STORAGE_PROVIDER
$awsAccessKey = $env:AWS_ACCESS_KEY_ID
$awsSecretKey = $env:AWS_SECRET_ACCESS_KEY

if ($storageProvider -eq "s3") {
    if ([string]::IsNullOrEmpty($awsAccessKey) -or [string]::IsNullOrEmpty($awsSecretKey)) {
        Write-Host "  S3 requested but credentials missing. Falling back to memory." -ForegroundColor Yellow
        $storageProvider = "memory"
    } else {
        Write-Host "  Using S3 with credentials" -ForegroundColor Green
    }
}

# Test 3: STORAGE_PROVIDER=s3 with credentials (should use S3)
Write-Host "`nTest 3: STORAGE_PROVIDER=s3 with credentials" -ForegroundColor Yellow
$env:STORAGE_PROVIDER = "s3"
$env:AWS_ACCESS_KEY_ID = "test-access-key"
$env:AWS_SECRET_ACCESS_KEY = "test-secret-key"
$env:AWS_REGION = "us-east-1"
$env:AWS_S3_BUCKET = "test-bucket"

$storageProvider = $env:STORAGE_PROVIDER
$awsAccessKey = $env:AWS_ACCESS_KEY_ID
$awsSecretKey = $env:AWS_SECRET_ACCESS_KEY

if ($storageProvider -eq "s3") {
    if ([string]::IsNullOrEmpty($awsAccessKey) -or [string]::IsNullOrEmpty($awsSecretKey)) {
        Write-Host "  S3 requested but credentials missing. Falling back to memory." -ForegroundColor Yellow
        $storageProvider = "memory"
    } else {
        Write-Host "  Using S3 with credentials" -ForegroundColor Green
        Write-Host "    Region: $env:AWS_REGION" -ForegroundColor Cyan
        Write-Host "    Bucket: $env:AWS_S3_BUCKET" -ForegroundColor Cyan
        if ($env:AWS_SERVICE_URL) {
            Write-Host "    Service URL: $env:AWS_SERVICE_URL" -ForegroundColor Cyan
        }
    }
}

# Test 4: STORAGE_PROVIDER=memory (should use memory)
Write-Host "`nTest 4: STORAGE_PROVIDER=memory" -ForegroundColor Yellow
$env:STORAGE_PROVIDER = "memory"
$storageProvider = $env:STORAGE_PROVIDER
Write-Host "  Using: $storageProvider" -ForegroundColor Green

Write-Host "`nAll S3 tests completed!" -ForegroundColor Green 