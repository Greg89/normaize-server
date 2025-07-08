# Test Storage Configuration Logic
Write-Host "Testing Storage Configuration Logic" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Test 1: No STORAGE_PROVIDER set (should default to memory)
Write-Host "`nTest 1: No STORAGE_PROVIDER set" -ForegroundColor Yellow
$env:STORAGE_PROVIDER = $null
$env:SFTP_HOST = $null
$env:SFTP_USERNAME = $null
$env:SFTP_PASSWORD = $null

$storageProvider = $env:STORAGE_PROVIDER
if ([string]::IsNullOrEmpty($storageProvider)) {
    $storageProvider = "memory"
    Write-Host "  Defaulting to: $storageProvider" -ForegroundColor Green
}

# Test 2: STORAGE_PROVIDER=sftp but no credentials (should fallback to memory)
Write-Host "`nTest 2: STORAGE_PROVIDER=sftp but no credentials" -ForegroundColor Yellow
$env:STORAGE_PROVIDER = "sftp"
$env:SFTP_HOST = $null
$env:SFTP_USERNAME = $null
$env:SFTP_PASSWORD = $null

$storageProvider = $env:STORAGE_PROVIDER
$sftpHost = $env:SFTP_HOST
$sftpUsername = $env:SFTP_USERNAME
$sftpPassword = $env:SFTP_PASSWORD

if ($storageProvider -eq "sftp") {
    if ([string]::IsNullOrEmpty($sftpHost) -or [string]::IsNullOrEmpty($sftpUsername) -or 
        ([string]::IsNullOrEmpty($sftpPassword) -and [string]::IsNullOrEmpty($env:SFTP_PRIVATE_KEY) -and [string]::IsNullOrEmpty($env:SFTP_PRIVATE_KEY_PATH))) {
        Write-Host "  SFTP requested but credentials missing. Falling back to memory." -ForegroundColor Yellow
        $storageProvider = "memory"
    } else {
        Write-Host "  Using SFTP with credentials" -ForegroundColor Green
    }
}

# Test 3: STORAGE_PROVIDER=sftp with credentials (should use SFTP)
Write-Host "`nTest 3: STORAGE_PROVIDER=sftp with credentials" -ForegroundColor Yellow
$env:STORAGE_PROVIDER = "sftp"
$env:SFTP_HOST = "161.35.10.105"
$env:SFTP_USERNAME = "normaize-sftp"
$env:SFTP_PASSWORD = "test-password"

$storageProvider = $env:STORAGE_PROVIDER
$sftpHost = $env:SFTP_HOST
$sftpUsername = $env:SFTP_USERNAME
$sftpPassword = $env:SFTP_PASSWORD

if ($storageProvider -eq "sftp") {
    if ([string]::IsNullOrEmpty($sftpHost) -or [string]::IsNullOrEmpty($sftpUsername) -or 
        ([string]::IsNullOrEmpty($sftpPassword) -and [string]::IsNullOrEmpty($env:SFTP_PRIVATE_KEY) -and [string]::IsNullOrEmpty($env:SFTP_PRIVATE_KEY_PATH))) {
        Write-Host "  SFTP requested but credentials missing. Falling back to memory." -ForegroundColor Yellow
        $storageProvider = "memory"
    } else {
        Write-Host "  Using SFTP with credentials" -ForegroundColor Green
    }
}

# Test 4: STORAGE_PROVIDER=memory (should use memory)
Write-Host "`nTest 4: STORAGE_PROVIDER=memory" -ForegroundColor Yellow
$env:STORAGE_PROVIDER = "memory"
$storageProvider = $env:STORAGE_PROVIDER
Write-Host "  Using: $storageProvider" -ForegroundColor Green

Write-Host "`nAll tests completed!" -ForegroundColor Green 