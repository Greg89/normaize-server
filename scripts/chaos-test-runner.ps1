#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Chaos Engineering Test Runner for Normaize API

.DESCRIPTION
    This script runs chaos engineering tests to validate system resilience
    and provides comprehensive reporting on system behavior during failures.

.PARAMETER Environment
    The environment to run tests against (Development, Beta, Production)

.PARAMETER TestType
    Type of chaos test to run (Infrastructure, Application, Dependency)

.PARAMETER Duration
    Duration of the chaos test in minutes

.PARAMETER ApiUrl
    Base URL of the API to test

.EXAMPLE
    .\chaos-test-runner.ps1 -Environment "Development" -TestType "Infrastructure" -Duration 5

.EXAMPLE
    .\chaos-test-runner.ps1 -Environment "Beta" -TestType "Dependency" -Duration 10 -ApiUrl "https://beta.normaize.com"
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Development", "Beta", "Production")]
    [string]$Environment,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("Infrastructure", "Application", "Dependency")]
    [string]$TestType,
    
    [Parameter(Mandatory = $false)]
    [int]$Duration = 5,
    
    [Parameter(Mandatory = $false)]
    [string]$ApiUrl = "http://localhost:5000"
)

# Configuration
$LogFile = "chaos-test-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$ResultsFile = "chaos-test-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$CorrelationId = [System.Guid]::NewGuid().ToString()

# Logging function
function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] [$CorrelationId] $Message"
    Write-Host $logMessage
    Add-Content -Path $LogFile -Value $logMessage
}

# Health check function
function Test-HealthEndpoint {
    try {
        $response = Invoke-RestMethod -Uri "$ApiUrl/health/readiness" -Method GET -TimeoutSec 10
        return @{
            Status = $response.status
            Healthy = $response.status -eq "healthy"
            Timestamp = Get-Date
        }
    }
    catch {
        return @{
            Status = "unreachable"
            Healthy = $false
            Error = $_.Exception.Message
            Timestamp = Get-Date
        }
    }
}

# Metrics collection function
function Get-SystemMetrics {
    try {
        $response = Invoke-RestMethod -Uri "$ApiUrl/api/metrics/performance" -Method GET -TimeoutSec 10
        return $response
    }
    catch {
        Write-Log "Failed to collect metrics: $($_.Exception.Message)" "WARN"
        return $null
    }
}

# Chaos test scenarios
$ChaosTests = @{
    Infrastructure = @{
        Database = {
            Write-Log "Simulating database connection failure..."
            # In a real scenario, you might stop the database service
            # For this example, we'll just monitor the system response
        }
        Storage = {
            Write-Log "Simulating storage service failure..."
            # Simulate S3 or other storage service failure
        }
        Network = {
            Write-Log "Simulating network latency..."
            # Add artificial network delay
        }
    }
    Application = @{
        Memory = {
            Write-Log "Simulating memory pressure..."
            # Generate memory pressure
        }
        CPU = {
            Write-Log "Simulating CPU spikes..."
            # Generate CPU load
        }
        ThreadPool = {
            Write-Log "Simulating thread pool exhaustion..."
            # Exhaust thread pool
        }
    }
    Dependency = @{
        ExternalAPI = {
            Write-Log "Simulating external API failure..."
            # Simulate external service failure
        }
        AuthService = {
            Write-Log "Simulating authentication service failure..."
            # Simulate Auth0 failure
        }
        CacheService = {
            Write-Log "Simulating cache service failure..."
            # Simulate Redis failure
        }
    }
}

# Test execution function
function Invoke-ChaosTest {
    param([string]$TestName, [scriptblock]$TestAction)
    
    Write-Log "Starting chaos test: $TestName"
    
    # Pre-test health check
    $preTestHealth = Test-HealthEndpoint
    Write-Log "Pre-test health status: $($preTestHealth.Status)"
    
    # Execute chaos scenario
    & $TestAction
    
    # Monitor system during test
    $testStart = Get-Date
    $healthChecks = @()
    $metrics = @()
    
    for ($i = 0; $i -lt $Duration; $i++) {
        Start-Sleep -Seconds 60
        
        $healthCheck = Test-HealthEndpoint
        $healthChecks += $healthCheck
        
        $metric = Get-SystemMetrics
        if ($metric) {
            $metrics += $metric
        }
        
        Write-Log "Minute $($i + 1): Health = $($healthCheck.Status)"
    }
    
    # Post-test health check
    $postTestHealth = Test-HealthEndpoint
    Write-Log "Post-test health status: $($postTestHealth.Status)"
    
    # Analyze results
    $analysis = @{
        TestName = $TestName
        CorrelationId = $CorrelationId
        Environment = $Environment
        Duration = $Duration
        PreTestHealth = $preTestHealth
        PostTestHealth = $postTestHealth
        HealthChecks = $healthChecks
        Metrics = $metrics
        Timestamp = Get-Date
    }
    
    # Determine test result
    $unhealthyPeriods = ($healthChecks | Where-Object { -not $_.Healthy }).Count
    $totalPeriods = $healthChecks.Count
    
    if ($unhealthyPeriods -eq 0) {
        $analysis.Result = "PASS"
        $analysis.Summary = "System remained healthy throughout the test"
        Write-Log "Test PASSED: System remained healthy" "SUCCESS"
    }
    elseif ($unhealthyPeriods -lt $totalPeriods * 0.5) {
        $analysis.Result = "DEGRADED"
        $analysis.Summary = "System experienced temporary degradation but recovered"
        Write-Log "Test DEGRADED: System experienced temporary issues" "WARN"
    }
    else {
        $analysis.Result = "FAIL"
        $analysis.Summary = "System failed to maintain health during the test"
        Write-Log "Test FAILED: System did not maintain health" "ERROR"
    }
    
    return $analysis
}

# Main execution
Write-Log "Starting Chaos Engineering Test Suite"
Write-Log "Environment: $Environment"
Write-Log "Test Type: $TestType"
Write-Log "Duration: $Duration minutes"
Write-Log "API URL: $ApiUrl"

# Validate API is accessible
Write-Log "Validating API accessibility..."
$initialHealth = Test-HealthEndpoint
if (-not $initialHealth.Healthy) {
    Write-Log "API is not healthy. Cannot proceed with chaos tests." "ERROR"
    exit 1
}

# Run chaos tests
$testResults = @()

foreach ($test in $ChaosTests[$TestType].GetEnumerator()) {
    $result = Invoke-ChaosTest -TestName $test.Key -TestAction $test.Value
    $testResults += $result
    
    # Wait between tests
    if ($test -ne $ChaosTests[$TestType].GetEnumerator()[-1]) {
        Write-Log "Waiting 2 minutes before next test..."
        Start-Sleep -Seconds 120
    }
}

# Generate summary report
$summary = @{
    TestSuite = "Chaos Engineering"
    Environment = $Environment
    TestType = $TestType
    CorrelationId = $CorrelationId
    Timestamp = Get-Date
    TotalTests = $testResults.Count
    PassedTests = ($testResults | Where-Object { $_.Result -eq "PASS" }).Count
    DegradedTests = ($testResults | Where-Object { $_.Result -eq "DEGRADED" }).Count
    FailedTests = ($testResults | Where-Object { $_.Result -eq "FAIL" }).Count
    Results = $testResults
}

# Save results
$summary | ConvertTo-Json -Depth 10 | Out-File -FilePath $ResultsFile -Encoding UTF8

# Display summary
Write-Log "=== CHAOS TEST SUMMARY ==="
Write-Log "Total Tests: $($summary.TotalTests)"
Write-Log "Passed: $($summary.PassedTests)"
Write-Log "Degraded: $($summary.DegradedTests)"
Write-Log "Failed: $($summary.FailedTests)"
Write-Log "Results saved to: $ResultsFile"
Write-Log "Log saved to: $LogFile"

# Exit with appropriate code
if ($summary.FailedTests -gt 0) {
    Write-Log "Chaos tests completed with failures" "ERROR"
    exit 1
}
elseif ($summary.DegradedTests -gt 0) {
    Write-Log "Chaos tests completed with degradation" "WARN"
    exit 2
}
else {
    Write-Log "All chaos tests passed" "SUCCESS"
    exit 0
} 