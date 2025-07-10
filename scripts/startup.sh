#!/bin/bash

# Startup script for Railway deployment
# This script runs migrations before starting the application

set -e

echo "Starting Normaize API deployment..."

# Check if we have database connection
if [ -n "$MYSQLHOST" ]; then
    echo "Database connection detected. Running migrations..."
    
    # Install EF Core tools if not present
    if ! command -v dotnet-ef &> /dev/null; then
        echo "Installing EF Core tools..."
        dotnet tool install --global dotnet-ef
    fi
    
    # Run migrations
    echo "Applying database migrations..."
    dotnet ef database update --project Normaize.Data --startup-project Normaize.API
    
    echo "Migrations completed successfully."
else
    echo "No database connection detected. Skipping migrations."
fi

# Start the application
echo "Starting application..."
exec dotnet Normaize.API.dll 