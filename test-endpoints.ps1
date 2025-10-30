# Quick API Endpoint Testing Script
# Tests common endpoints to verify the API is working

param(
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "http://localhost:8080"
)

Write-Host "🧪 Testing Normaize API Endpoints" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor White
Write-Host ""

# Function to test an endpoint
function Test-Endpoint 
{
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET"
    )
    
    Write-Host "Testing: $Name" -ForegroundColor Yellow -NoNewline
    Write-Host " ($Method $Url)" -ForegroundColor Gray
    
    try 
    {
        $response = Invoke-WebRequest -Uri $Url -Method $Method -TimeoutSec 10 -ErrorAction Stop
        Write-Host "  ✅ Status: $($response.StatusCode)" -ForegroundColor Green
        
        # Try to parse as JSON
        try 
        {
            $json = $response.Content | ConvertFrom-Json
            Write-Host "  📄 Response:" -ForegroundColor Cyan
            Write-Host ($json | ConvertTo-Json -Depth 2) -ForegroundColor Gray
        } 
        catch 
        {
            Write-Host "  📄 Response: $($response.Content.Substring(0, [Math]::Min(200, $response.Content.Length)))..." -ForegroundColor Gray
        }
        Write-Host ""
        return $true
    } 
    catch 
    {
        Write-Host "  ❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        return $false
    }
}

# Test endpoints
$results = @{}

Write-Host "1️⃣ Health Checks" -ForegroundColor Magenta
Write-Host "─────────────────" -ForegroundColor Magenta
$results['Health'] = Test-Endpoint -Name "Main Health Check" -Url "$BaseUrl/health"
$results['HealthReady'] = Test-Endpoint -Name "Ready Health Check" -Url "$BaseUrl/health/ready"
$results['HealthLive'] = Test-Endpoint -Name "Live Health Check" -Url "$BaseUrl/health/live"

Write-Host "2️⃣ API Documentation" -ForegroundColor Magenta
Write-Host "─────────────────────" -ForegroundColor Magenta
$results['Swagger'] = Test-Endpoint -Name "Swagger UI" -Url "$BaseUrl/"
$results['SwaggerJson'] = Test-Endpoint -Name "Swagger JSON" -Url "$BaseUrl/swagger/v1/swagger.json"

# Summary
Write-Host ""
Write-Host "📊 Test Summary" -ForegroundColor Cyan
Write-Host "═══════════════" -ForegroundColor Cyan

$passed = ($results.Values | Where-Object { $_ -eq $true }).Count
$total = $results.Count
$failed = $total - $passed

Write-Host ""
Write-Host "Total Tests: $total" -ForegroundColor White
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($failed -eq 0) 
{
    Write-Host "🎉 All tests passed! API is working correctly." -ForegroundColor Green
} 
else 
{
    Write-Host "⚠️ Some tests failed. Check the output above for details." -ForegroundColor Yellow
    Write-Host "   Run '.\test-docker.ps1 -Logs' to view API logs" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Quick Links:" -ForegroundColor Cyan
Write-Host "   Swagger UI: ${BaseUrl}" -ForegroundColor White
Write-Host "   Health: ${BaseUrl}/health" -ForegroundColor White
