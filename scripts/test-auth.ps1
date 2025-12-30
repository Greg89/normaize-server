# Test Authentication Flow
Write-Host "Testing Normaize API Authentication" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Test 1: Check if API is running
Write-Host "`n1. Testing API availability..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/health/readiness" -Method GET -TimeoutSec 5
    Write-Host "✅ API is running (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ API is not running. Please start the API first." -ForegroundColor Red
    Write-Host "Run: dotnet run --project Normaize.API" -ForegroundColor Cyan
    exit 1
}

# Test 2: Test login endpoint
Write-Host "`n2. Testing login endpoint..." -ForegroundColor Yellow
$loginData = @{
    username = "test@normaize.com"
    password = "TestPassword123!"
} | ConvertTo-Json

Write-Host "   Using credentials: test@normaize.com / TestPassword123!" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" -Method POST -Body $loginData -ContentType "application/json"
    $tokenResponse = $response.Content | ConvertFrom-Json
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "   Token: $($tokenResponse.token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host "   Expires in: $($tokenResponse.expiresIn) seconds" -ForegroundColor Gray
    
    $token = $tokenResponse.token
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorContent = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorContent)
        $errorBody = $reader.ReadToEnd()
        Write-Host "   Error details: $errorBody" -ForegroundColor Red
    }
    exit 1
}

# Test 3: Test authenticated endpoint
Write-Host "`n3. Testing authenticated endpoint..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Accept" = "application/json"
    }
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/test" -Method GET -Headers $headers
    $testResponse = $response.Content | ConvertFrom-Json
    Write-Host "✅ Authentication test successful!" -ForegroundColor Green
    Write-Host "   User ID: $($testResponse.userId)" -ForegroundColor Gray
    Write-Host "   Message: $($testResponse.message)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Authentication test failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 4: Test datasets endpoint
Write-Host "`n4. Testing datasets endpoint..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Accept" = "application/json"
    }
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/datasets" -Method GET -Headers $headers
    Write-Host "✅ Datasets endpoint accessible!" -ForegroundColor Green
    Write-Host "   Status: $($response.StatusCode)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Datasets endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n🎉 All tests passed! Authentication is working correctly." -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Open Swagger UI: http://localhost:5000/swagger" -ForegroundColor White
Write-Host "2. Use the /api/auth/login endpoint to get a token" -ForegroundColor White
Write-Host "3. Click 'Authorize' in Swagger and enter: Bearer <your-token>" -ForegroundColor White
Write-Host "4. Test the protected endpoints!" -ForegroundColor White 