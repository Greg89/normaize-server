#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Pre-commit validation script for Normaize project
    
.DESCRIPTION
    Runs essential checks before committing code:
    - Solution build validation
    - Unit tests execution
    - Code formatting verification
    - Basic security checks
    
.PARAMETER SkipTests
    Skip running unit tests (useful for quick validation)
    
.PARAMETER SkipFormat
    Skip code formatting checks
    
.PARAMETER Verbose
    Enable verbose output
#>

param(
    [switch]$SkipTests,
    [switch]$SkipFormat,
    [switch]$Verbose
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Function to write colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [System.ConsoleColor]$Color = [System.ConsoleColor]::White
    )
    Write-Host $Message -ForegroundColor $Color
}

# Function to write section header
function Write-Section {
    param([string]$Title)
    Write-ColorOutput "`n" ([System.ConsoleColor]::White)
    Write-ColorOutput ("=" * 60) ([System.ConsoleColor]::Blue)
    Write-ColorOutput " $Title" ([System.ConsoleColor]::Blue)
    Write-ColorOutput ("=" * 60) ([System.ConsoleColor]::Blue)
    Write-ColorOutput "" ([System.ConsoleColor]::White)
}

# Function to write step header
function Write-Step {
    param([string]$Step)
    Write-ColorOutput "`n>> $Step" ([System.ConsoleColor]::Yellow)
}

# Function to check if command exists
function Test-Command {
    param([string]$Command)
    try {
        Get-Command $Command -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

# Initialize
Write-Section "Normaize Pre-Commit Validation"
Write-ColorOutput "Starting pre-commit validation..." ([System.ConsoleColor]::White)

# Get script directory and navigate to root
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
Set-Location $rootPath
Write-ColorOutput "Working directory: $(Get-Location)" ([System.ConsoleColor]::Blue)

# Track overall success
$overallSuccess = $true
$startTime = Get-Date

# 1. Check prerequisites
Write-Step "Checking Prerequisites"

# Check .NET SDK
if (-not (Test-Command "dotnet")) {
    Write-ColorOutput "ERROR: .NET SDK not found. Please install .NET 9.0 or later." ([System.ConsoleColor]::Red)
    exit 1
}

$dotnetVersion = dotnet --version
Write-ColorOutput "SUCCESS: .NET SDK: $dotnetVersion" ([System.ConsoleColor]::Green)

# Check if solution file exists
if (-not (Test-Path "Normaize.sln")) {
    Write-ColorOutput "ERROR: Solution file not found. Please run from project root." ([System.ConsoleColor]::Red)
    exit 1
}

Write-ColorOutput "SUCCESS: Solution file found" ([System.ConsoleColor]::Green)

# 2. Restore packages
Write-Step "Restoring NuGet Packages"
try {
    if ($Verbose) {
        dotnet restore --verbosity normal
    } else {
        dotnet restore --verbosity minimal
    }
    Write-ColorOutput "SUCCESS: Packages restored successfully" ([System.ConsoleColor]::Green)
}
catch {
    Write-ColorOutput "ERROR: Package restore failed: $($_.Exception.Message)" ([System.ConsoleColor]::Red)
    $overallSuccess = $false
}

# 3. Build solution
Write-Step "Building Solution"
try {
    if ($Verbose) {
        dotnet build --verbosity normal --no-restore
    } else {
        dotnet build --verbosity minimal --no-restore
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "SUCCESS: Solution built successfully" ([System.ConsoleColor]::Green)
    } else {
        Write-ColorOutput "ERROR: Build failed" ([System.ConsoleColor]::Red)
        $overallSuccess = $false
    }
}
catch {
    Write-ColorOutput "ERROR: Build failed: $($_.Exception.Message)" ([System.ConsoleColor]::Red)
    $overallSuccess = $false
}

# 4. Run tests (if not skipped)
if (-not $SkipTests) {
    Write-Step "Running Unit Tests"
    try {
        if ($Verbose) {
            dotnet test --verbosity normal --no-build
        } else {
            dotnet test --verbosity minimal --no-build
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "SUCCESS: All tests passed" ([System.ConsoleColor]::Green)
        } else {
            Write-ColorOutput "ERROR: Some tests failed" ([System.ConsoleColor]::Red)
            $overallSuccess = $false
        }
    }
    catch {
        Write-ColorOutput "ERROR: Test execution failed: $($_.Exception.Message)" ([System.ConsoleColor]::Red)
        $overallSuccess = $false
    }
} else {
    Write-ColorOutput "SKIPPED: Tests skipped" ([System.ConsoleColor]::Yellow)
}

# 5. Code formatting check (if not skipped)
if (-not $SkipFormat) {
    Write-Step "Checking Code Formatting"
    try {
        # Check if dotnet format is available
        try {
            dotnet format --help | Out-Null
            $formatAvailable = $true
        } catch {
            $formatAvailable = $false
        }
        
        if ($formatAvailable) {
            # Check formatting without making changes
            dotnet format --verify-no-changes --verbosity quiet
            
            if ($LASTEXITCODE -eq 0) {
                Write-ColorOutput "SUCCESS: Code formatting is correct" ([System.ConsoleColor]::Green)
            } else {
                Write-ColorOutput "ERROR: Code formatting issues found. Run 'dotnet format' to fix." ([System.ConsoleColor]::Red)
                $overallSuccess = $false
            }
        } else {
            Write-ColorOutput "WARNING: dotnet format not available. Install with: dotnet tool install -g dotnet-format" ([System.ConsoleColor]::Yellow)
        }
    }
    catch {
        Write-ColorOutput "ERROR: Format check failed: $($_.Exception.Message)" ([System.ConsoleColor]::Red)
        $overallSuccess = $false
    }
} else {
    Write-ColorOutput "SKIPPED: Format check skipped" ([System.ConsoleColor]::Yellow)
}

# 6. Security check
Write-Step "Security Validation"
try {
    # Check for outdated packages
    $outdatedResult = dotnet list package --outdated 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        # Check if the output contains "no updates" which means no outdated packages
        if ($outdatedResult -and ($outdatedResult -join "`n") -match "no updates given the current sources") {
            Write-ColorOutput "SUCCESS: No outdated packages found" ([System.ConsoleColor]::Green)
        } else {
            Write-ColorOutput "WARNING: Found outdated packages. Consider updating." ([System.ConsoleColor]::Yellow)
        }
    } else {
        Write-ColorOutput "WARNING: Could not check for outdated packages" ([System.ConsoleColor]::Yellow)
    }
    
    # Check for deprecated packages
    $deprecatedResult = dotnet list package --deprecated 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        $deprecatedOutput = $deprecatedResult -join "`n"
        # Check if the output contains "has the following deprecated packages" which means there are deprecated packages
        if ($deprecatedOutput -match "has the following deprecated packages") {
            Write-ColorOutput "WARNING: Found deprecated packages." ([System.ConsoleColor]::Yellow)
        } else {
            Write-ColorOutput "SUCCESS: No deprecated packages found" ([System.ConsoleColor]::Green)
        }
    } else {
        Write-ColorOutput "WARNING: Could not check for deprecated packages" ([System.ConsoleColor]::Yellow)
    }
}
catch {
    Write-ColorOutput "ERROR: Security validation failed: $($_.Exception.Message)" ([System.ConsoleColor]::Red)
    $overallSuccess = $false
}

# Calculate duration
$endTime = Get-Date
$duration = $endTime - $startTime

# Final results
Write-Section "Validation Results"
Write-ColorOutput "Duration: $($duration.ToString('mm\:ss'))" ([System.ConsoleColor]::White)

if ($overallSuccess) {
    Write-ColorOutput "SUCCESS: All checks passed! Ready to commit." ([System.ConsoleColor]::Green)
    Write-ColorOutput "`nNext steps:" ([System.ConsoleColor]::Blue)
    Write-ColorOutput "1. Stage your changes: git add ." ([System.ConsoleColor]::White)
    Write-ColorOutput "2. Commit with a meaningful message: git commit -m 'Your message'" ([System.ConsoleColor]::White)
    Write-ColorOutput "3. Push to repository: git push" ([System.ConsoleColor]::White)
    exit 0
} else {
    Write-ColorOutput "ERROR: Some checks failed. Please fix the issues before committing." ([System.ConsoleColor]::Red)
    Write-ColorOutput "`nCommon fixes:" ([System.ConsoleColor]::Blue)
    Write-ColorOutput "• Run 'dotnet format' to fix formatting issues" ([System.ConsoleColor]::White)
    Write-ColorOutput "• Fix failing tests" ([System.ConsoleColor]::White)
    Write-ColorOutput "• Resolve build errors" ([System.ConsoleColor]::White)
    exit 1
} 