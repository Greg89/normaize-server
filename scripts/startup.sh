#!/bin/bash

# Startup script for Railway deployment
# This script runs migrations before starting the application

set -e

echo "Starting Normaize API deployment..."

# Check if we have database connection
if [ -n "$MYSQLHOST" ]; then
    echo "Database connection detected. Running migrations..."
    
    # Check if EF Core tools are available
    if command -v dotnet-ef &> /dev/null; then
        echo "EF Core tools found in PATH"
        EF_TOOL="dotnet-ef"
    elif [ -f "/tools/dotnet-ef" ]; then
        echo "EF Core tools found in /tools directory"
        EF_TOOL="/tools/dotnet-ef"
    else
        echo "ERROR: EF Core tools not found. Available tools:"
        ls -la /tools/ 2>/dev/null || echo "No /tools directory found"
        echo "PATH: $PATH"
        exit 1
    fi
    
    # Change to the API project directory (where the startup project is)
    cd /app/Normaize.API
    
    # Run migrations
    echo "Applying database migrations using: $EF_TOOL"
    echo "Working directory: $(pwd)"
    echo "Project files:"
    ls -la *.csproj 2>/dev/null || echo "No .csproj files found in current directory"
    
    $EF_TOOL database update --project ../Normaize.Data --startup-project .
    
    echo "Migrations completed successfully."
else
    echo "No database connection detected. Skipping migrations."
fi

# Start the application (from the root /app directory)
cd /app
echo "Starting application..."
exec dotnet Normaize.API.dll 