@echo off
setlocal enabledelayedexpansion

REM Pre-commit validation script for Normaize project
REM Runs essential checks before committing code

echo.
echo ================================================================================
echo  🚀 Normaize Pre-Commit Validation
echo ================================================================================
echo.

REM Get script directory and navigate to root
set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%SCRIPT_DIR%.."
cd /d "%ROOT_DIR%"

echo 📁 Working directory: %CD%
echo.

REM Check prerequisites
echo ▶ Checking Prerequisites

REM Check if dotnet is available
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ❌ .NET SDK not found. Please install .NET 9.0 or later.
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo ✅ .NET SDK: !DOTNET_VERSION!

REM Check if solution file exists
if not exist "Normaize.sln" (
    echo ❌ Solution file not found. Please run from project root.
    exit /b 1
)
echo ✅ Solution file found
echo.

REM Restore packages
echo ▶ Restoring NuGet Packages
dotnet restore --verbosity minimal
if errorlevel 1 (
    echo ❌ Package restore failed
    exit /b 1
)
echo ✅ Packages restored successfully
echo.

REM Build solution
echo ▶ Building Solution
dotnet build --verbosity minimal --no-restore
if errorlevel 1 (
    echo ❌ Build failed
    exit /b 1
)
echo ✅ Solution built successfully
echo.

REM Run tests
echo ▶ Running Unit Tests
dotnet test --verbosity minimal --no-build
if errorlevel 1 (
    echo ❌ Some tests failed
    exit /b 1
)
echo ✅ All tests passed
echo.

REM Check code formatting
echo ▶ Checking Code Formatting
dotnet format --verify-no-changes --verbosity quiet
if errorlevel 1 (
    echo ❌ Code formatting issues found. Run 'dotnet format' to fix.
    exit /b 1
)
echo ✅ Code formatting is correct
echo.

REM Check for outdated packages
echo ▶ Security Validation
dotnet list package --outdated >nul 2>&1
if errorlevel 1 (
    echo ✅ No outdated packages found
) else (
    echo ⚠️  Found outdated packages. Consider updating.
)

REM Check for deprecated packages
dotnet list package --deprecated >nul 2>&1
if errorlevel 1 (
    echo ✅ No deprecated packages found
) else (
    echo ⚠️  Found deprecated packages.
)
echo.

REM Final results
echo ================================================================================
echo  📊 Validation Results
echo ================================================================================
echo.
echo 🎉 All checks passed! Ready to commit.
echo.
echo Next steps:
echo 1. Stage your changes: git add .
echo 2. Commit with a meaningful message: git commit -m "Your message"
echo 3. Push to repository: git push
echo.

exit /b 0 