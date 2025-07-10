#!/bin/bash

# Startup script for Railway deployment
# This script runs migrations before starting the application

set -e

echo "Starting Normaize API deployment..."

# Check if we have database connection
if [ -n "$MYSQLHOST" ]; then
    echo "Database connection detected. Running migrations..."
    
    # Run migrations (EF Core tools are pre-installed in the image)
    echo "Applying database migrations..."
    dotnet ef database update --project Normaize.Data --startup-project Normaize.API
    
    echo "Migrations completed successfully."
else
    echo "No database connection detected. Skipping migrations."
fi

# Start the application
echo "Starting application..."
exec dotnet Normaize.API.dll 