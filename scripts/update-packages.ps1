# Update Packages Script for Normaize Solution
# This script updates all outdated packages across all projects

param(
    [switch]$DryRun = $false,
    [switch]$SkipTests = $false
)

Write-Host "Checking for outdated packages..." -ForegroundColor Cyan

# Get all project files
$projectFiles = Get-ChildItem -Path . -Filter "*.csproj" -Recurse

# Get outdated packages for each project
$outdatedPackages = @()

foreach ($project in $projectFiles) {
    Write-Host "Checking $($project.Name)..." -ForegroundColor Yellow
    
    $output = dotnet list $project.FullName package --outdated --format json 2>$null
    if ($LASTEXITCODE -eq 0) {
        $json = $output | ConvertFrom-Json
        
        foreach ($projectData in $json.projects) {
            foreach ($framework in $projectData.frameworks) {
                foreach ($package in $framework.topLevelPackages) {
                    $outdatedPackages += [PSCustomObject]@{
                        Project = $project.Name
                        Package = $package.id
                        CurrentVersion = $package.resolvedVersion
                        LatestVersion = $package.latestVersion
                        ProjectPath = $project.FullName
                    }
                }
            }
        }
    }
}

if ($outdatedPackages.Count -eq 0) {
    Write-Host "All packages are up to date!" -ForegroundColor Green
    exit 0
}

Write-Host "`nFound $($outdatedPackages.Count) outdated packages:" -ForegroundColor Yellow

# Group by project for better display
$groupedPackages = $outdatedPackages | Group-Object Project

foreach ($group in $groupedPackages) {
    Write-Host "`n$($group.Name):" -ForegroundColor Magenta
    foreach ($package in $group.Group) {
        Write-Host "  • $($package.Package): $($package.CurrentVersion) -> $($package.LatestVersion)" -ForegroundColor White
    }
}

if ($DryRun) {
    Write-Host "`nDry run mode - no packages will be updated" -ForegroundColor Cyan
    exit 0
}

Write-Host "`nStarting package updates..." -ForegroundColor Green

# Update packages by project
$successCount = 0
$errorCount = 0

foreach ($group in $groupedPackages) {
    Write-Host "`nUpdating $($group.Name)..." -ForegroundColor Magenta
    
    foreach ($package in $group.Group) {
        Write-Host "  Updating $($package.Package)..." -ForegroundColor Yellow
        
        try {
            $result = dotnet add $package.ProjectPath package $package.Package --version $package.LatestVersion
            if ($LASTEXITCODE -eq 0) {
                Write-Host "    Updated $($package.Package) to $($package.LatestVersion)" -ForegroundColor Green
                $successCount++
            } else {
                Write-Host "    Failed to update $($package.Package)" -ForegroundColor Red
                $errorCount++
            }
        }
        catch {
            Write-Host "    Error updating $($package.Package): $($_.Exception.Message)" -ForegroundColor Red
            $errorCount++
        }
    }
}

Write-Host "`nUpdate Summary:" -ForegroundColor Cyan
Write-Host "  Successfully updated: $successCount packages" -ForegroundColor Green
Write-Host "  Failed updates: $errorCount packages" -ForegroundColor Red

if ($errorCount -gt 0) {
    Write-Host "`nSome packages failed to update. Check the errors above." -ForegroundColor Yellow
}

# Restore packages
Write-Host "`nRestoring packages..." -ForegroundColor Cyan
dotnet restore

if ($LASTEXITCODE -eq 0) {
    Write-Host "Package restore completed successfully" -ForegroundColor Green
} else {
    Write-Host "Package restore failed" -ForegroundColor Red
}

# Build solution
Write-Host "`nBuilding solution..." -ForegroundColor Cyan
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build completed successfully" -ForegroundColor Green
} else {
    Write-Host "Build failed" -ForegroundColor Red
}

# Run tests (unless skipped)
if (-not $SkipTests) {
    Write-Host "`nRunning tests..." -ForegroundColor Cyan
    dotnet test --verbosity normal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "All tests passed" -ForegroundColor Green
    } else {
        Write-Host "Some tests failed" -ForegroundColor Red
    }
} else {
    Write-Host "`nSkipping tests (SkipTests flag used)" -ForegroundColor Yellow
}

Write-Host "`nPackage update process completed!" -ForegroundColor Green 