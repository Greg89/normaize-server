# Version Helper Script
# This script helps analyze commit messages and determine version bump types

param(
    [string]$Action = "analyze",
    [string]$Version = "",
    [string]$BumpType = ""
)

function Get-CurrentVersion {
    $csprojFile = "Normaize.API/Normaize.API.csproj"
    if (Test-Path $csprojFile) {
        $content = Get-Content $csprojFile -Raw
        if ($content -match '<Version>(.*?)</Version>') {
            return $matches[1]
        }
    }
    return "1.0.0"
}

function Get-CommitMessages {
    param([int]$Days = 1)
    
    $commits = git log --oneline --since="$Days days ago" | Select-Object -First 10
    return $commits
}

function Analyze-CommitMessages {
    param([string[]]$Commits)
    
    $majorKeywords = @("breaking", "major", "!:", "BREAKING CHANGE")
    $minorKeywords = @("feat", "feature", "new", "enhancement")
    
    foreach ($commit in $commits) {
        $commitLower = $commit.ToLower()
        
        # Check for major version indicators
        foreach ($keyword in $majorKeywords) {
            if ($commitLower -like "*$keyword*") {
                Write-Host "Major version bump detected due to: $commit" -ForegroundColor Red
                return "major"
            }
        }
        
        # Check for minor version indicators
        foreach ($keyword in $minorKeywords) {
            if ($commitLower -like "*$keyword*") {
                Write-Host "Minor version bump detected due to: $commit" -ForegroundColor Yellow
                return "minor"
            }
        }
    }
    
    Write-Host "Patch version bump (default)" -ForegroundColor Green
    return "patch"
}

function Calculate-NewVersion {
    param(
        [string]$CurrentVersion,
        [string]$BumpType
    )
    
    $parts = $CurrentVersion.Split('.')
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]
    
    switch ($BumpType) {
        "major" {
            $major++
            $minor = 0
            $patch = 0
        }
        "minor" {
            $minor++
            $patch = 0
        }
        "patch" {
            $patch++
        }
    }
    
    return "$major.$minor.$patch"
}

function Update-ProjectVersions {
    param([string]$NewVersion)
    
    $projectFiles = @(
        "Normaize.API/Normaize.API.csproj",
        "Normaize.Core/Normaize.Core.csproj",
        "Normaize.Data/Normaize.Data.csproj",
        "Normaize.Tests/Normaize.Tests.csproj"
    )
    
    foreach ($file in $projectFiles) {
        if (Test-Path $file) {
            $content = Get-Content $file -Raw
            $updatedContent = $content -replace '<Version>.*?</Version>', "<Version>$NewVersion</Version>"
            Set-Content $file $updatedContent -NoNewline
            Write-Host "Updated $file to version $NewVersion" -ForegroundColor Green
        }
    }
}

# Main script logic
switch ($Action) {
    "analyze" {
        Write-Host "=== Version Analysis ===" -ForegroundColor Cyan
        $currentVersion = Get-CurrentVersion
        Write-Host "Current version: $currentVersion" -ForegroundColor White
        
        $commits = Get-CommitMessages
        Write-Host "`nRecent commits:" -ForegroundColor Cyan
        $commits | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        
        $bumpType = Analyze-CommitMessages $commits
        $newVersion = Calculate-NewVersion $currentVersion $bumpType
        
        Write-Host "`n=== Version Bump Summary ===" -ForegroundColor Cyan
        Write-Host "Current version: $currentVersion" -ForegroundColor White
        Write-Host "Bump type: $bumpType" -ForegroundColor Yellow
        Write-Host "New version: $newVersion" -ForegroundColor Green
        
        # Output for GitHub Actions
        Write-Host "::set-output name=current-version::$currentVersion"
        Write-Host "::set-output name=bump-type::$bumpType"
        Write-Host "::set-output name=new-version::$newVersion"
    }
    
    "update" {
        if ([string]::IsNullOrEmpty($Version)) {
            Write-Host "Error: Version parameter is required for update action" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "Updating project versions to $Version..." -ForegroundColor Cyan
        Update-ProjectVersions $Version
        Write-Host "Version update completed!" -ForegroundColor Green
    }
    
    "show" {
        $currentVersion = Get-CurrentVersion
        Write-Host "Current version: $currentVersion" -ForegroundColor Green
    }
    
    default {
        Write-Host "Usage: ./scripts/version-helper.ps1 [analyze|update|show] [version] [bump-type]" -ForegroundColor Yellow
        Write-Host "" -ForegroundColor White
        Write-Host "Actions:" -ForegroundColor Cyan
        Write-Host "  analyze  - Analyze commits and determine version bump (default)" -ForegroundColor White
        Write-Host "  update   - Update all project files to specified version" -ForegroundColor White
        Write-Host "  show     - Show current version" -ForegroundColor White
        Write-Host "" -ForegroundColor White
        Write-Host "Examples:" -ForegroundColor Cyan
        Write-Host "  ./scripts/version-helper.ps1" -ForegroundColor White
        Write-Host "  ./scripts/version-helper.ps1 update 1.2.3" -ForegroundColor White
        Write-Host "  ./scripts/version-helper.ps1 show" -ForegroundColor White
    }
} 