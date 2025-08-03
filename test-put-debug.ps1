# Debug script for PUT endpoint authentication issues
param(
    [string]$Token = "",
    [int]$DatasetId = 1,
    [string]$BaseUrl = "http://localhost:5000"
)

Write-Host "=== PUT Endpoint Debug Test ===" -ForegroundColor Cyan
Write-Host ""

if ([string]::IsNullOrEmpty($Token)) {
    Write-Host "ERROR: No token provided. Please provide a valid JWT token." -ForegroundColor Red
    Write-Host "Usage: .\test-put-debug.ps1 -Token 'your_jwt_token_here'" -ForegroundColor Yellow
    exit 1
}

Write-Host "Testing PUT endpoint: $BaseUrl/api/datasets/$DatasetId" -ForegroundColor Green
Write-Host "Token preview: $($Token.Substring(0, [Math]::Min(50, $Token.Length)))..." -ForegroundColor Gray
Write-Host ""

# Test 1: Check if the API is running
Write-Host "1. Testing API health..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-WebRequest -Uri "$BaseUrl/api/health" -Method GET -UseBasicParsing
    Write-Host "   ✓ API is running (Status: $($healthResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   ✗ API is not running: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Test CORS preflight
Write-Host "2. Testing CORS preflight..." -ForegroundColor Yellow
try {
    $corsResponse = Invoke-WebRequest -Uri "$BaseUrl/api/datasets/$DatasetId" -Method OPTIONS -Headers @{
        "Origin" = "http://localhost:3000"
        "Access-Control-Request-Method" = "PUT"
        "Access-Control-Request-Headers" = "Content-Type,Authorization"
    } -UseBasicParsing
    Write-Host "   ✓ CORS preflight successful (Status: $($corsResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   ✗ CORS preflight failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Test GET endpoint (should work with same token)
Write-Host "3. Testing GET endpoint with same token..." -ForegroundColor Yellow
try {
    $getResponse = Invoke-WebRequest -Uri "$BaseUrl/api/datasets/$DatasetId" -Method GET -Headers @{
        "Authorization" = "Bearer $Token"
    } -UseBasicParsing
    Write-Host "   ✓ GET endpoint works (Status: $($getResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   ✗ GET endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   This suggests a token issue!" -ForegroundColor Red
}

# Test 4: Test PUT endpoint
Write-Host "4. Testing PUT endpoint..." -ForegroundColor Yellow
$putBody = @{
    name = "Test Update $(Get-Date -Format 'HH:mm:ss')"
    description = "Test description from debug script"
} | ConvertTo-Json

try {
    $putResponse = Invoke-WebRequest -Uri "$BaseUrl/api/datasets/$DatasetId" -Method PUT -Headers @{
        "Authorization" = "Bearer $Token"
        "Content-Type" = "application/json"
    } -Body $putBody -UseBasicParsing
    Write-Host "   ✓ PUT endpoint works! (Status: $($putResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response: $($putResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   ✗ PUT endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "   Status Code: $statusCode" -ForegroundColor Red
        
        if ($statusCode -eq 401) {
            Write-Host "   This is an authentication error!" -ForegroundColor Red
            Write-Host "   Check the server logs for detailed JWT authentication errors." -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=== Debug Summary ===" -ForegroundColor Cyan
Write-Host "If PUT fails but GET works, check:" -ForegroundColor Yellow
Write-Host "1. Token expiration between requests" -ForegroundColor White
Write-Host "2. Different scopes required for PUT vs GET" -ForegroundColor White
Write-Host "3. Server logs for JWT authentication errors" -ForegroundColor White
Write-Host "4. CORS configuration for PUT requests" -ForegroundColor White 