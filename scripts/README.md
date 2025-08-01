# Pre-Commit Scripts

This directory contains scripts to validate your code before committing to the repository.

## Available Scripts

### PowerShell Script (Recommended)
- **File**: `pre-commit.ps1`
- **Platform**: Windows, macOS, Linux (with PowerShell Core)
- **Features**: Comprehensive validation with colored output

### Batch Script (Windows)
- **File**: `pre-commit.bat`
- **Platform**: Windows only
- **Features**: Basic validation with simple output

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

## What the Scripts Check

### Essential Checks (Both Scripts)
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

## Troubleshooting

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

### Modifying Validation Rules
Edit the script to adjust:
- File patterns to scan
- Validation thresholds
- Error messages
- Success criteria

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