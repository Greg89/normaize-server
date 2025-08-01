# Pre-Commit Scripts

This directory contains scripts to validate your code before committing to the repository.

## Available Scripts

### Pre-Commit Validation Scripts

#### PowerShell Script (Recommended)
- **File**: `pre-commit.ps1`
- **Platform**: Windows, macOS, Linux (with PowerShell Core)
- **Features**: Comprehensive validation with colored output

#### Batch Script (Windows)
- **File**: `pre-commit.bat`
- **Platform**: Windows only
- **Features**: Basic validation with simple output

### Package Update Scripts

#### PowerShell Script (Recommended)
- **File**: `update-packages.ps1`
- **Platform**: Windows, macOS, Linux (with PowerShell Core)
- **Features**: Automated package updates with comprehensive validation

#### Batch Script (Windows)
- **File**: `update-packages.bat`
- **Platform**: Windows only
- **Features**: Manual package updates with predefined versions

## Usage

### PowerShell Script

```powershell
# Run all checks (recommended)
.\scripts\pre-commit.ps1

# Skip tests for quick validation
.\scripts\pre-commit.ps1 -SkipTests

# Skip formatting checks
.\scripts\pre-commit.ps1 -SkipFormat

# Enable verbose output
.\scripts\pre-commit.ps1 -Verbose

# Combine options
.\scripts\pre-commit.ps1 -SkipTests -Verbose
```

### Batch Script

```cmd
# Run all checks
scripts\pre-commit.bat
```

## Package Update Usage

### PowerShell Script (Recommended)

The PowerShell script automatically detects and updates all outdated packages across all projects in the solution.

```powershell
# Check for outdated packages (dry run)
powershell -ExecutionPolicy Bypass -File scripts/update-packages.ps1 -DryRun

# Update all packages
powershell -ExecutionPolicy Bypass -File scripts/update-packages.ps1

# Update packages but skip tests
powershell -ExecutionPolicy Bypass -File scripts/update-packages.ps1 -SkipTests

# Update packages with verbose output
powershell -ExecutionPolicy Bypass -File scripts/update-packages.ps1 -Verbose
```

### Batch Script

The batch script updates packages to specific predefined versions.

```cmd
# Update all packages to latest versions
scripts\update-packages.bat
```

### Manual Package Updates

For selective package updates or custom version management:

```bash
# Check for outdated packages
dotnet list package --outdated

# Update a specific package in a project
dotnet add Normaize.API/Normaize.API.csproj package PackageName --version LatestVersion

# Update all packages in a project (example)
dotnet add Normaize.API/Normaize.API.csproj package CsvHelper --version 33.1.0
dotnet add Normaize.API/Normaize.API.csproj package DotNetEnv --version 3.1.1
```

### Using Visual Studio

1. Right-click on your solution in Solution Explorer
2. Select "Manage NuGet Packages for Solution"
3. Go to the "Updates" tab
4. Select packages to update and click "Update"

### Using dotnet CLI with Global Tool

```bash
# Install the dotnet-outdated tool
dotnet tool install --global dotnet-outdated-tool

# Check for outdated packages
dotnet outdated

# Update packages
dotnet outdated --upgrade
```

## What the Scripts Check

### Pre-Commit Validation Checks

#### Essential Checks (Both Scripts)
1. **Prerequisites**
   - .NET SDK availability
   - Solution file existence

2. **Package Management**
   - NuGet package restoration

3. **Build Validation**
   - Solution compilation
   - Error detection

4. **Testing**
   - Unit test execution
   - Test result validation

5. **Code Formatting**
   - Format verification (requires `dotnet format` tool)
   - Style consistency

6. **Security**
   - Outdated package detection
   - Deprecated package warnings

### Advanced Checks (PowerShell Only)
7. **Code Analysis**
   - TODO/FIXME comment detection
   - Potential hardcoded secrets
   - Code quality patterns

8. **Documentation**
   - README existence
   - XML documentation coverage

9. **Performance**
   - Async method patterns
   - Memory usage patterns

### Package Update Process

#### PowerShell Script Features
1. **Package Detection**
   - Scans all projects for outdated packages
   - Identifies current vs. latest versions
   - Groups packages by project

2. **Automated Updates**
   - Updates packages to latest versions
   - Handles dependencies automatically
   - Provides progress feedback

3. **Validation Steps**
   - Package restoration
   - Solution building
   - Test execution
   - Success/failure reporting

4. **Safety Features**
   - Dry run mode for preview
   - Error handling and reporting
   - Rollback capability through git

#### Batch Script Features
1. **Predefined Updates**
   - Updates to specific known versions
   - Covers all major packages
   - Consistent across environments

2. **Simple Execution**
   - One-command execution
   - No complex parameters
   - Windows-native experience

## Prerequisites

### Required Tools
- **.NET 9.0 SDK** or later
- **Git** (for version control)

### Optional Tools
- **dotnet-format** (for formatting checks)
  ```bash
  dotnet tool install -g dotnet-format
  ```

## Integration with Git

### Manual Usage
Run the script before committing:
```bash
# Stage your changes
git add .

# Run pre-commit validation
.\scripts\pre-commit.ps1

# If successful, commit
git commit -m "Your commit message"
```

### Git Hooks (Advanced)
You can integrate the script with Git hooks for automatic validation:

1. Create a pre-commit hook:
```bash
# Copy the script to .git/hooks/
cp scripts/pre-commit.ps1 .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

2. The hook will run automatically before each commit

## Package Update Best Practices

### Before Updating
1. **Backup Your Work**
   ```bash
   # Commit current state
   git add .
   git commit -m "Backup before package updates"
   ```

2. **Check Current State**
   ```bash
   # Verify current package versions
   dotnet list package --outdated
   ```

3. **Review Breaking Changes**
   - Check package release notes
   - Review major version changes
   - Test in a separate branch first

### During Updates
1. **Use Dry Run First**
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts/update-packages.ps1 -DryRun
   ```

2. **Update Incrementally**
   - Update one project at a time
   - Test after each major update
   - Address issues before continuing

3. **Monitor for Errors**
   - Watch for build failures
   - Check for breaking changes
   - Review test failures

### After Updates
1. **Verify Functionality**
   ```bash
   # Run all tests
   dotnet test --verbosity normal
   
   # Build solution
   dotnet build
   ```

2. **Check for Issues**
   - Review build warnings
   - Address deprecated APIs
   - Update code if needed

3. **Document Changes**
   - Update package versions in documentation
   - Note any breaking changes
   - Update team on new features

## Troubleshooting

### Package Update Issues

#### Build Failures After Updates
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build

# Check for specific errors
dotnet build --verbosity normal
```

#### Test Failures After Updates
```bash
# Run tests with detailed output
dotnet test --verbosity normal --logger "console;verbosity=detailed"

# Check for specific test failures
dotnet test --filter "TestClassName"
```

#### Package Conflicts
```bash
# Check package dependencies
dotnet list package --include-transitive

# Resolve conflicts manually
dotnet add package PackageName --version SpecificVersion
```

#### Breaking Changes
```bash
# Revert to previous version if needed
git checkout HEAD~1 -- Normaize.API/Normaize.API.csproj
dotnet restore
```

### Common Issues

#### Build Failures
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

#### Test Failures
```bash
# Run tests with verbose output
dotnet test --verbosity normal
```

#### Formatting Issues
```bash
# Fix formatting automatically
dotnet format
```

#### Missing dotnet-format Tool
```bash
# Install the formatting tool
dotnet tool install -g dotnet-format
```

### Script-Specific Issues

#### PowerShell Execution Policy
If you get execution policy errors:
```powershell
# Check current policy
Get-ExecutionPolicy

# Set policy for current user (if needed)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### Batch File Issues
If the batch file doesn't work:
- Ensure you're running from the project root
- Check that .NET SDK is in your PATH
- Verify the solution file exists

## Customization

### Adding Custom Checks
You can extend the PowerShell script by adding new validation steps:

```powershell
# Add your custom check here
Write-Step "Custom Validation"
try {
    # Your validation logic
    Write-ColorOutput "✅ Custom check passed" $Green
}
catch {
    Write-ColorOutput "❌ Custom check failed" $Red
    $overallSuccess = $false
}
```

### Customizing Package Updates

#### Modifying Update Scripts
You can customize the package update scripts for your specific needs:

```powershell
# Add custom package update logic
Write-Host "Updating custom packages..." -ForegroundColor Yellow

# Update specific packages with custom logic
$customPackages = @(
    @{ Name = "CustomPackage"; Version = "1.0.0" },
    @{ Name = "AnotherPackage"; Version = "2.0.0" }
)

foreach ($package in $customPackages) {
    dotnet add $projectPath package $package.Name --version $package.Version
}
```

#### Adding Package Validation
```powershell
# Add custom package validation
function Test-PackageCompatibility {
    param($PackageName, $Version)
    
    # Your compatibility logic here
    $compatible = $true
    
    if (-not $compatible) {
        Write-Host "Warning: $PackageName $Version may have compatibility issues" -ForegroundColor Yellow
    }
    
    return $compatible
}
```

### Modifying Validation Rules
Edit the script to adjust:
- File patterns to scan
- Validation thresholds
- Error messages
- Success criteria

## Current Package Status

### Typical Outdated Packages

The Normaize solution typically has the following packages that need regular updates:

#### Normaize.API
- **CsvHelper**: 31.0.0 → 33.1.0
- **DotNetEnv**: 3.0.0 → 3.1.1
- **EPPlus**: 7.0.9 → 8.0.8
- **Microsoft.AspNetCore.Authentication.JwtBearer**: 9.0.0 → 9.0.7
- **Serilog.AspNetCore**: 8.0.0 → 9.0.0
- **Swashbuckle.AspNetCore**: 7.0.0 → 9.0.3

#### Normaize.Core
- **AutoMapper**: 12.0.1 → 15.0.1
- **FluentValidation**: 11.8.1 → 12.0.0
- **Microsoft.Extensions.* packages**: 9.0.0 → 9.0.7
- **Serilog**: 3.1.1 → 4.3.0

#### Normaize.Data
- **AWSSDK.S3**: 3.7.305.12 → 4.0.6.2
- **Microsoft.EntityFrameworkCore**: 9.0.0 → 9.0.7

#### Normaize.Tests
- **FluentAssertions**: 6.12.0 → 8.5.0
- **xunit**: 2.7.0 → 2.9.3
- **Moq**: 4.20.70 → 4.20.72

### Update Frequency

- **Security Updates**: Immediately when available
- **Patch Updates**: Monthly
- **Minor Updates**: Quarterly
- **Major Updates**: After thorough testing

## Best Practices

1. **Run Regularly**: Use the script before every commit
2. **Fix Issues**: Address all warnings and errors before committing
3. **Keep Updated**: Update the script as your project evolves
4. **Team Adoption**: Encourage team members to use the script
5. **CI Integration**: Consider running similar checks in your CI pipeline

## Contributing

When modifying the scripts:
1. Test thoroughly on different platforms
2. Update this documentation
3. Consider backward compatibility
4. Add appropriate error handling
5. Maintain consistent output formatting 