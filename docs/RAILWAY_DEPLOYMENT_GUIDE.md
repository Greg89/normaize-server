# Railway Deployment Guide for Normaize

## Overview

This guide explains how database migrations and MySQL optimizations work when deploying to Railway with your MySQL database.

## How Railway Deployment Works

### **Automatic Migration Process**

When your application deploys to Railway:

1. **Container Starts**: Railway builds and starts your Docker container
2. **Application Initializes**: Your `Program.cs` runs
3. **Database Check**: App detects Railway MySQL environment variables
4. **EF Core Migration**: Automatically applies pending migrations
5. **MySQL Optimizations**: Applies performance optimizations (first time only)
6. **Application Starts**: Your API becomes available

### **Environment Variables in Railway**

Railway automatically provides these MySQL environment variables:
- `MYSQLHOST` - Database host
- `MYSQLDATABASE` - Database name
- `MYSQLUSER` - Database user
- `MYSQLPASSWORD` - Database password
- `MYSQLPORT` - Database port

## Deployment Flow

### **Step 1: Code Push**
```bash
git push origin main
```

### **Step 2: Railway Build**
```dockerfile
# Dockerfile builds your application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# ... build process ...
COPY --from=build /src/Normaize.Data/Migrations ./Migrations/
```

### **Step 3: Container Startup**
```csharp
// Program.cs automatically runs migrations
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLHOST")))
{
    // Apply EF Core migrations
    context.Database.Migrate();
    
    // Apply MySQL optimizations
    await ApplyMySqlOptimizationsAsync(context);
}
```

### **Step 4: Database Changes Applied**
- ✅ New tables created (`DataSetRows`)
- ✅ New columns added to existing tables
- ✅ Indexes created for performance
- ✅ Stored procedures and views added

## Railway-Specific Configuration

### **Connection String Optimization**
```csharp
var connectionString = $"Server={Environment.GetEnvironmentVariable("MYSQLHOST")};" +
                      $"Database={Environment.GetEnvironmentVariable("MYSQLDATABASE")};" +
                      $"User={Environment.GetEnvironmentVariable("MYSQLUSER")};" +
                      $"Password={Environment.GetEnvironmentVariable("MYSQLPASSWORD")};" +
                      $"Port={Environment.GetEnvironmentVariable("MYSQLPORT")};" +
                      "CharSet=utf8mb4;" +
                      "AllowLoadLocalInfile=true;" +
                      "Convert Zero Datetime=True;" +
                      "Allow Zero Datetime=True;";
```

### **Migration File Inclusion**
```dockerfile
# Copy migration files to container
COPY --from=build /src/Normaize.Data/Migrations ./Migrations/
```

## Environment-Specific Behavior

### **Beta Environment**
```csharp
// Swagger enabled for testing
if (app.Environment.IsDevelopment() || 
    app.Environment.EnvironmentName.Equals("Beta", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

### **Production Environment**
```csharp
// Swagger disabled, migrations still run
// Optimizations applied only once
```

## Migration Safety Features

### **Error Handling**
```csharp
try
{
    context.Database.Migrate();
    await ApplyMySqlOptimizationsAsync(context);
}
catch (Exception ex)
{
    Log.Error(ex, "Error applying database migrations");
    // Don't throw - allow application to start
}
```

### **Optimization Check**
```csharp
// Check if optimizations already applied
var optimizationsApplied = await context.Database.SqlQueryRaw<bool>(
    "SELECT COUNT(*) > 0 FROM information_schema.tables " +
    "WHERE table_schema = DATABASE() AND table_name = 'DataSetStatistics'"
).FirstOrDefaultAsync();
```

## Monitoring and Logging

### **Migration Logs**
```csharp
Log.Information("Applying database migrations...");
Log.Information("Database migrations applied successfully");
Log.Information("MySQL optimizations applied successfully");
```

### **Railway Logs**
You can view migration progress in Railway's deployment logs:
```bash
# In Railway dashboard
railway logs
```

## Adding New Migrations

### **1. Create New Migration**
```bash
cd Normaize.Data
dotnet ef migrations add NewFeature --startup-project ../Normaize.API
```

### **2. Update Optimizations (Optional)**
```sql
-- Add to MySQL_Optimizations.sql
CREATE INDEX IF NOT EXISTS idx_new_feature ON DataSets(NewColumn);
CREATE PROCEDURE IF NOT EXISTS GetNewFeature();
```

### **3. Deploy to Railway**
```bash
git add .
git commit -m "Add new database migration"
git push origin main
```

### **4. Automatic Application**
Railway automatically applies the new migration when the container starts.

## Troubleshooting

### **Migration Fails**
```bash
# Check Railway logs
railway logs

# Common issues:
# 1. Database connection issues
# 2. Insufficient permissions
# 3. Migration conflicts
```

### **Optimizations Not Applied**
```sql
-- Check if optimizations table exists
SELECT COUNT(*) FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'DataSetStatistics';
```

### **Connection Issues**
```bash
# Verify Railway environment variables
railway variables

# Check database connectivity
railway connect
```

## Railway Commands

### **View Deployment Status**
```bash
railway status
```

### **View Logs**
```bash
railway logs
```

### **Connect to Database**
```bash
railway connect
```

### **Check Environment Variables**
```bash
railway variables
```

## Best Practices

### **1. Test Locally First**
```bash
# Test migration locally
dotnet ef database update --startup-project ../Normaize.API

# Test optimizations
mysql -u root -p normaize < Normaize.Data/Migrations/MySQL_Optimizations.sql
```

### **2. Use Feature Flags**
```csharp
// Only apply certain optimizations in production
if (Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT") == "production")
{
    // Apply production-specific optimizations
}
```

### **3. Monitor Performance**
```sql
-- Check if indexes are being used
EXPLAIN SELECT * FROM DataSets WHERE UploadedAt > '2024-01-01';
```

### **4. Backup Before Major Changes**
```bash
# Railway provides automatic backups, but you can also:
railway backup
```

## Migration Rollback

### **If Migration Fails**
```bash
# Railway will show error in logs
# Fix the issue and redeploy
git push origin main
```

### **Manual Rollback (if needed)**
```sql
-- Drop problematic tables/columns
DROP TABLE IF EXISTS DataSetRows;
ALTER TABLE DataSets DROP COLUMN IF EXISTS NewColumn;
```

## Summary

**Railway Deployment Process:**
1. ✅ **Code Push** → Triggers Railway build
2. ✅ **Container Build** → Includes migration files
3. ✅ **Container Start** → Program.cs runs
4. ✅ **Migration Check** → Detects Railway MySQL
5. ✅ **EF Core Migration** → Applies schema changes
6. ✅ **MySQL Optimizations** → Applies performance enhancements
7. ✅ **Application Start** → API becomes available

**Key Benefits:**
- 🚀 **Fully Automated**: No manual database changes needed
- 🔒 **Safe**: Error handling prevents deployment failures
- 📊 **Monitored**: Comprehensive logging for troubleshooting
- 🔄 **Idempotent**: Safe to run multiple times
- ⚡ **Fast**: Optimizations applied only when needed

This approach ensures your database changes are automatically applied every time you deploy to Railway! 🎯 